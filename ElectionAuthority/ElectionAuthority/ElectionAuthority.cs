﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Windows.Forms;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Generators;

namespace ElectionAuthority
{
    class ElectionAuthority
    {
        ASCIIEncoding encoder;
        private Logs logs;
        private Form1 form;
        private Server serverClient; // server for clients (voters)
        public Server ServerClient
        {
            get { return serverClient; }
        }
        private Server serverProxy; // server for proxy
        public Server ServerProxy
        {
            get { return serverProxy; }
        }

        private CandidateList candidateList;
        private List<String> candidateDefaultList;
        private Configuration configuration;

        //permutation PI
        private Permutation permutation;
        private List<List<BigInteger>> permutationsList;
        private List<List<BigInteger>> inversePermutationList;

        //serial number SL
        private List<BigInteger> serialNumberList;

        //tokens, one SL has four tokens
        private List<List<BigInteger>> tokensList;          //for (un)blinding (proxy and EA)
        private List<List<BigInteger>> exponentsList;       //for blinding (proxy)
        private List<List<BigInteger>> signatureFactor;     //for signature (EA)

        //map which connect serialNumber and permuataion
        private Dictionary<BigInteger, List<BigInteger>> dictionarySLPermuation;
        private Dictionary<BigInteger, List<BigInteger>> dictionarySLInversePermutation;
        private Dictionary<BigInteger, List<List<BigInteger>>> dictionarySLTokens;

        private Dictionary<string, Ballot> ballots;         //EA reprezentation of every Voter ballot

        private int numberOfVoters;
        private int[] finalResults;


        private Auditor auditor;                            // check if voting process runs with all 
        private RsaKeyParameters privKey;                   // priv Key to bit commitment of permutation
        private RsaKeyParameters pubKey;                    // pub key to bit commitment of permutation

        //Tokens (for blind signature scheme)
        private List<BigInteger> permutationTokensList;
        private List<BigInteger> permutationExponentsList;


        public ElectionAuthority(Logs logs, Configuration configuration, Form1 form)
        {
            this.encoder = new ASCIIEncoding();
            this.logs = logs;
            this.configuration = configuration;
            this.form = form;
            //server for Clients
            this.serverClient = new Server(this.logs, this);


            //server for Proxy
            this.serverProxy = new Server(this.logs, this);

            this.numberOfVoters = Convert.ToInt32(this.configuration.NumberOfVoters);
            permutation = new Permutation(this.logs);

            this.ballots = new Dictionary<string, Ballot>();

            this.auditor = new Auditor(this.logs);

            //init key pair generator (for RSA bit-commitment)
            KeyGenerationParameters para = new KeyGenerationParameters(new SecureRandom(), 1024);
            RsaKeyPairGenerator keyGen = new RsaKeyPairGenerator();
            keyGen.Init(para);


            //generate key pair and get keys (for bit-commitment)
            AsymmetricCipherKeyPair keypair = keyGen.GenerateKeyPair();
            privKey = (RsaKeyParameters)keypair.Private;
            pubKey = (RsaKeyParameters)keypair.Public;
        }

        public void loadCandidateList(string pathToElectionAuthorityConfig)
        {
            //pathToElectionAuthorityConfig it's a path to file which contains ElectionAuthority config
            //we have to rewrite one to be suitiable for list candidate xml
            candidateDefaultList = new List<String>();
            candidateList = new CandidateList(this.logs);

            string pathToCandidateList = candidateList.getPathToCandidateList(pathToElectionAuthorityConfig);
            candidateDefaultList = candidateList.loadCanidateList(pathToCandidateList);
        }

        //** Start methods to generate tokens and permutation 
        private void generatePermutation()
        {
            //generating permutation and feeling List
            permutationsList = new List<List<BigInteger>>();
            for (int i = 0; i < this.numberOfVoters; i++)
            {
                this.permutationsList.Add(new List<BigInteger>(this.permutation.generatePermutation(candidateDefaultList.Count)));
            }

            connectSerialNumberAndPermutation();
            generateInversePermutation();
            generatePermutationTokens();
            blindPermutation(permutationsList);              //Send commited permutation to Auditor
            logs.addLog(Constants.PERMUTATION_GEN_SUCCESSFULLY, true, Constants.LOG_INFO);

        }

        private void generatePermutationTokens()
        {
            this.permutationTokensList = new List<BigInteger>();
            this.permutationExponentsList = new List<BigInteger>();


            for (int i = 0; i < this.numberOfVoters; i++)
            { // we use the same method like to generate serial number, there is another random generator used inside this method
                List<AsymmetricCipherKeyPair> preToken = new List<AsymmetricCipherKeyPair>(SerialNumberGenerator.generatePreTokens(1, Constants.NUMBER_OF_BITS_TOKEN));

                RsaKeyParameters publicKey = (RsaKeyParameters)preToken[0].Public;
                RsaKeyParameters privKey = (RsaKeyParameters)preToken[0].Private;
                permutationTokensList.Add(publicKey.Modulus);
                permutationExponentsList.Add(publicKey.Exponent);
            }
            Console.WriteLine("Permutation tokens generated");
        }

        private void generateInversePermutation()
        {
            //using mathematics to generate inverse permutation for our List
            this.inversePermutationList = new List<List<BigInteger>>();
            for (int i = 0; i < this.numberOfVoters; i++)
            {
                this.inversePermutationList.Add(this.permutation.getInversePermutation(this.permutationsList[i]));
            }
            logs.addLog(Constants.GENERATE_INVERSE_PERMUTATION, true, Constants.LOG_INFO, true);
            connectSerialNumberAndInversePermutation();

        }

        private void connectSerialNumberAndInversePermutation()
        {
            dictionarySLInversePermutation = new Dictionary<BigInteger, List<BigInteger>>();
            for (int i = 0; i < this.serialNumberList.Count; i++)
            {
                dictionarySLInversePermutation.Add(this.serialNumberList[i], this.inversePermutationList[i]);
            }
            logs.addLog(Constants.SL_CONNECTED_WITH_INVERSE_PERMUTATION, true, Constants.LOG_INFO, true);
        }

        private void generateSerialNumber()
        {
            //Generating serial numbers (SL)
            serialNumberList = new List<BigInteger>();
            serialNumberList = SerialNumberGenerator.generateListOfSerialNumber(this.numberOfVoters, Constants.NUMBER_OF_BITS_SL);

            logs.addLog(Constants.SERIAL_NUMBER_GEN_SUCCESSFULLY, true, Constants.LOG_INFO, true);
        }

        private void generateTokens()
        {

            //preparing Big Integers for RSA blind signature (token have to fulfil requirments) 
            this.tokensList = new List<List<BigInteger>>();
            this.exponentsList = new List<List<BigInteger>>();
            this.signatureFactor = new List<List<BigInteger>>();


            for (int i = 0; i < this.numberOfVoters; i++)
            { // we use the same method like to generate serial number, there is another random generator used inside this method
                List<AsymmetricCipherKeyPair> preToken = new List<AsymmetricCipherKeyPair>(SerialNumberGenerator.generatePreTokens(4, Constants.NUMBER_OF_BITS_TOKEN));
                List<BigInteger> tokens = new List<BigInteger>();
                List<BigInteger> exps = new List<BigInteger>();
                List<BigInteger> signFactor = new List<BigInteger>();

                foreach (AsymmetricCipherKeyPair token in preToken)
                {
                    RsaKeyParameters publicKey = (RsaKeyParameters)token.Public;
                    RsaKeyParameters privKey = (RsaKeyParameters)token.Private;
                    tokens.Add(publicKey.Modulus);
                    exps.Add(publicKey.Exponent);
                    signFactor.Add(privKey.Exponent);
                }
                this.tokensList.Add(tokens);
                this.exponentsList.Add(exps);
                this.signatureFactor.Add(signFactor);
            }


            logs.addLog(Constants.TOKENS_GENERATED_SUCCESSFULLY, true, Constants.LOG_INFO, true);
            connectSerialNumberAndTokens();

        }

        private void connectSerialNumberAndPermutation()
        {
            dictionarySLPermuation = new Dictionary<BigInteger, List<BigInteger>>();
            for (int i = 0; i < this.serialNumberList.Count; i++)
            {
                dictionarySLPermuation.Add(this.serialNumberList[i], this.permutationsList[i]);
            }
            logs.addLog(Constants.SL_CONNECTED_WITH_PERMUTATION, true, Constants.LOG_INFO);

        }

        private void connectSerialNumberAndTokens()
        {
            this.dictionarySLTokens = new Dictionary<BigInteger, List<List<BigInteger>>>();
            for (int i = 0; i < this.serialNumberList.Count; i++)
            {
                List<List<BigInteger>> tokens = new List<List<BigInteger>>();
                tokens.Add(tokensList[i]);
                tokens.Add(exponentsList[i]);
                tokens.Add(signatureFactor[i]);
                this.dictionarySLTokens.Add(this.serialNumberList[i], tokens);
            }

            logs.addLog(Constants.SL_CONNECTED_WITH_TOKENS, true, Constants.LOG_INFO);
        }

        public void generateDate()
        {
            generateSerialNumber();
            generateTokens();
            generatePermutation();

        }
        //** End methods to generate tokens and permutation 

        public void sendSLAndTokensToProxy()
        {
            //before sending we have to convert dictionary to string. We use our own conversion to recoginize message in proxy and reparse it to dictionary

            string msg = Constants.SL_TOKENS + "&";
            for (int i = 0; i < this.serialNumberList.Count; i++)
            {

                msg = msg + this.serialNumberList[i].ToString() + "=";
                for (int j = 0; j < this.tokensList[i].Count; j++)
                {
                    if (j == this.tokensList[i].Count - 1)
                        msg = msg + this.tokensList[i][j].ToString() + ":";

                    else
                        msg = msg + this.tokensList[i][j].ToString() + ",";

                }

                for (int j = 0; j < this.exponentsList[i].Count; j++)
                {
                    if (j == this.exponentsList[i].Count - 1)
                        msg += this.exponentsList[i][j].ToString();

                    else
                        msg = msg + this.exponentsList[i][j].ToString() + ",";

                }

                if (i != this.serialNumberList.Count - 1)
                    msg += ";";

            }
            this.serverProxy.sendMessage(Constants.PROXY, msg);
        }

        public void disableSendSLTokensAndTokensButton()
        {
            this.form.Invoke(new MethodInvoker(delegate()
                {
                    this.form.disableSendSLTokensAndTokensButton();
                }));

        }

        public void getCandidateListPermuated(string name, BigInteger SL)
        {
            List<BigInteger> permutation = new List<BigInteger>();
            permutation = this.dictionarySLPermuation[SL];

            List<String> candidateList = new List<string>();

            for (int i = 0; i < this.candidateDefaultList.Count; i++)
            {
                int index = permutation[i].IntValue;
                candidateList.Add(candidateDefaultList[index - 1]);
            }

            string candidateListString = Constants.CANDIDATE_LIST_RESPONSE + "&";

            for (int i = 0; i < candidateList.Count; i++)
            {
                if (i < candidateList.Count - 1)
                    candidateListString += candidateList[i] + ";";
                else
                    candidateListString += candidateList[i];
            }

            this.serverClient.sendMessage(name, candidateListString);

        }

        public void saveBlindBallotMatrix(string message)
        {
            //saving data recived from Proxy

            string[] words = message.Split(';');

            //1st parameter = name of voter
            string name = words[0];

            //2nd = SL of VOTER
            BigInteger SL = new BigInteger(words[1]);

            //Then tokens
            List<BigInteger> tokenList = new List<BigInteger>();
            string[] strTokens = words[2].Split(',');
            for (int i = 0; i < strTokens.Length; i++)
            {
                tokenList.Add(new BigInteger(strTokens[i]));
            }

            //Exponent list (used for blind signature)
            List<BigInteger> exponentList = new List<BigInteger>();
            string[] strExpo = words[3].Split(',');
            for (int i = 0; i < strExpo.Length; i++)
            {
                exponentList.Add(new BigInteger(strExpo[i]));
            }

            //and at least voted colums
            BigInteger[] columns = new BigInteger[4];
            string[] strColumns = words[4].Split(',');
            for (int i = 0; i < columns.Length; i++)
            {
                columns[i] = new BigInteger(strColumns[i]);
            }

            this.ballots.Add(name, new Ballot(SL));

            this.ballots[name].BlindColumn = columns;
            this.ballots[name].Permutation = this.dictionarySLPermuation[SL];
            this.ballots[name].InversePermutation = this.dictionarySLInversePermutation[SL];
            this.ballots[name].TokenList = tokenList;
            this.ballots[name].ExponentsList = exponentList;
            this.ballots[name].SignatureFactor = this.dictionarySLTokens[SL][2];

            this.logs.addLog(Constants.BLIND_PROXY_BALLOT_RECEIVED + name, true, Constants.LOG_INFO, true);

            this.signColumn(name);
        }

        private void signColumn(string name)
        {
            //msg = BLIND_PROXY_BALLOT&name;signCol1,signCol2,signCol3,signCol4
            this.ballots[name].signColumn();
            string signColumns = null;

            for (int i = 0; i < this.ballots[name].SignedColumn.Length; i++)
            {
                if (i == this.ballots[name].SignedColumn.Length - 1)
                    signColumns += this.ballots[name].SignedColumn[i].ToString();
                else
                    signColumns = signColumns + this.ballots[name].SignedColumn[i].ToString() + ",";
            }

            string msg = Constants.SIGNED_PROXY_BALLOT + "&" + name + ";" + signColumns;
            this.serverProxy.sendMessage(Constants.PROXY, msg);
            this.logs.addLog(Constants.SIGNED_BALLOT_MATRIX_SENT, true, Constants.LOG_INFO, true);
        }



        public void saveUnblindedBallotMatrix(string message)
        {
            //the same as previous saving
            string[] words = message.Split(';');

            string name = words[0];
            string[] strUnblinedColumns = words[1].Split(',');

            string[,] unblindedBallot = new string[this.candidateDefaultList.Count, Constants.BALLOT_SIZE];
            for (int i = 0; i < strUnblinedColumns.Length; i++)
            {
                for (int j = 0; j < strUnblinedColumns[i].Length; j++)
                {
                    unblindedBallot[j, i] = strUnblinedColumns[i][j].ToString();
                }
            }

            string[,] unblindedUnpermuatedBallot = new string[this.candidateDefaultList.Count, Constants.BALLOT_SIZE];
            BigInteger[] inversePermutation = this.ballots[name].InversePermutation.ToArray();

            for (int i = 0; i < unblindedUnpermuatedBallot.GetLength(0); i++)
            {
                string strRow = inversePermutation[i].ToString();
                int row = Convert.ToInt32(strRow) - 1;
                for (int j = 0; j < unblindedUnpermuatedBallot.GetLength(1); j++)
                {
                    unblindedUnpermuatedBallot[i, j] = unblindedBallot[row, j];
                }
            }

            this.ballots[name].UnblindedBallot = unblindedUnpermuatedBallot;
            this.logs.addLog(Constants.UNBLINED_BALLOT_MATRIX_RECEIVED, true, Constants.LOG_INFO, true);
        }

        public void disbaleProxy()
        {
            try
            {
                this.serverProxy.stopServer();
            }
            catch (Exception)
            {
                this.logs.addLog(Constants.UNABLE_TO_STOP_VOTING, true, Constants.LOG_ERROR, true);
            }

            this.logs.addLog(Constants.VOTIGN_STOPPED, true, Constants.LOG_INFO, true);

        }

        public void countVotes()
        {
            //counting votes

            /*ublindPermutation - EA send to voter unblinded permutation (and then private key) so Audiotr
                can check RSA formula*/
            unblindPermutation(permutationsList);

            this.finalResults = new int[this.candidateDefaultList.Count];
            initializeFinalResults();

            for (int i = 0; i < this.ballots.Count; i++)
            {
                int signleVote = checkVote(i);
                if (signleVote != -1)
                {
                    this.finalResults[signleVote] += 1;
                }
            }

            this.announceResultsOfElection();
        }

        private void announceResultsOfElection()
        {

            int maxValue = this.finalResults.Max();
            int maxIndex = this.finalResults.ToList().IndexOf(maxValue);
            int winningCandidates = 0;
            string winners = null;
            for (int i = 0; i < this.finalResults.Length; i++)
            {
                if (this.finalResults[i] == maxValue)
                {
                    winningCandidates += 1; // a few candidates has the same number of votes.
                    winners = winners + this.candidateDefaultList[i] + " ";
                }
            }

            if (winningCandidates == 1)
            {
                this.form.Invoke(new MethodInvoker(delegate()
                    {
                        MessageBox.Show("Winner of the election is: " + winners);
                    }));

            }
            else
            {
                this.form.Invoke(new MethodInvoker(delegate()
                {

                    MessageBox.Show("There is no one winner. Candidates on first place ex aequo: " + winners);
                }));

            }

        }

        private int checkVote(int voterNumber)
        {
            Ballot ballot = this.ballots.ElementAt(voterNumber).Value;
            string[,] vote = ballot.UnblindedBallot;
            Console.WriteLine("Voter number " + voterNumber);
            int voteCastOn = -1;
            for (int i = 0; i < vote.GetLength(0); i++)
            {
                int numberOfYes = 0;
                for (int j = 0; j < vote.GetLength(1); j++)
                {
                    if (vote[i, j] == "1")
                        numberOfYes += 1;
                }


                if (numberOfYes == 3)
                {
                    voteCastOn = i;
                    break;
                }
            }

            return voteCastOn;

        }


        private void initializeFinalResults()
        {
            for (int i = 0; i < this.finalResults.Length; i++)
            {
                this.finalResults[i] = 0;
            }
        }



        //Auditor's functions
        public void blindPermutation(List<List<BigInteger>> permutationList)
        {

            int size = permutationList.Count;
            BigInteger[] toSend = new BigInteger[size];

            //preparing List of permutation to send
            int k = 0;
            string[] strPermuationList = new string[permutationList.Count];
            foreach (List<BigInteger> list in permutationList)
            {
                string str = null;
                foreach (BigInteger big in list)
                {
                    str += big.ToString();
                }
                strPermuationList[k] = str;
                k++;
            }

            //RSA formula (bit commitment)
            int i = 0;
            foreach (string str in strPermuationList)
            {
                BigInteger toBlind = new BigInteger(str);
                BigInteger e = pubKey.Exponent;
                BigInteger n = pubKey.Modulus;
                BigInteger b = toBlind.ModPow(e, n);
                toSend[i] = b;
                i++;
            }
            this.auditor.CommitedPermatation = toSend;
        }

        public void unblindPermutation(List<List<BigInteger>> permutationList)
        {
            int size = permutationList.Count;
            BigInteger[] toSend = new BigInteger[size];

            int k = 0;
            string[] strPermuationList = new string[permutationList.Count];

            foreach (List<BigInteger> list in permutationList)
            {
                string str = null;
                foreach (BigInteger big in list)
                {
                    str += big.ToString();
                }
                strPermuationList[k] = str;
                k++;
            }

            int i = 0;
            foreach (string str in strPermuationList)
            {
                BigInteger b = new BigInteger(str);
                toSend[i] = b;
                i++;
            }


            //checking permutations RSA (auditor checks all of the permutations)
            if (this.auditor.checkPermutation(this.privKey, this.pubKey, toSend))
            {
                logs.addLog(Constants.BIT_COMMITMENT_OK, true, Constants.LOG_INFO, true);
            }
            else
            {
                logs.addLog(Constants.BIT_COMMITMENT_FAIL, true, Constants.LOG_ERROR, true);
            }
        }
    }
}

