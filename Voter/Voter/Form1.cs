﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Voter
{
    public partial class Form1 : Form
    {

        private Logs logs;
        private Configuration configuration;

        private Client electionAuthorityClient;
        private Client proxyClient;

        private VoterBallot ballot;

        public Form1()
        {
            InitializeComponent();
            setColumnWidth();
            this.logs = new Logs(this.logsListView);
            this.configuration = new Configuration(this.logs);
            this.electionAuthorityClient = new Client(this.configuration.Name, this.logs);
            this.proxyClient = new Client(this.configuration.Name , this.logs);
        }

        private void EAConnectButton_Click(object sender, EventArgs e)
        {
            this.electionAuthorityClient.connect(this.configuration.ElectionAuthorityIP, this.configuration.ElectionAuthorityPort, Constants.ELECTION_AUTHORITY);
            this.configButton.Enabled = false;
        }

        private void voteButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            String[] words = clickedButton.Name.Split(';');

            if (this.ballot.vote(Convert.ToInt32(words[1]), Convert.ToInt32(words[2])))
            {
                logs.addLog(Constants.VOTE_DONE, true, Constants.LOG_INFO, true);
            }
            else
            {
                logs.addLog(Constants.VOTE_ERROR, true, Constants.LOG_ERROR, true);
            }

            //Console.WriteLine(words[0] );

        }

        private void setColumnWidth()
        {
            this.logColumn.Width = this.logsListView.Width - 5;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.electionAuthorityClient.disconnectFromElectionAuthority();
        }

        private void ProxyConnectButton_Click(object sender, EventArgs e)
        {
            this.proxyClient.connect(configuration.ProxyIP, configuration.ProxyPort, Constants.PROXY);

        }

        private void configButton_Click(object sender, EventArgs e)
        {
            openFileDialog.ShowDialog();
        }

        private void openFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            configuration.loadConfiguration(openFileDialog.FileName);
            enableButtonsAfterLoadingConfiguration();

            this.ballot = new VoterBallot(configuration.NumberOfCandidates);
            addFieldsForCandidates(configuration.NumberOfCandidates);

        }

        private void addFieldsForCandidates(int NumberOfCandidates)
        {
            for (int i = 0; i < NumberOfCandidates; i++)
            {
                TextBox newTextBox = new TextBox();
                textBoxes.Add(newTextBox);

                Button[] newVoteButtons = new Button[Constants.BALLOTSIZE];
                for (int it = 0; it < Constants.BALLOTSIZE; it++)
                {
                    Button newCandidateButton = new Button();
                    newVoteButtons[it] = newCandidateButton;
                }

                voteButtons.Add(newVoteButtons);
                this.panel1.Controls.Add(newTextBox);
                this.textBoxes[i].Location = new System.Drawing.Point(23, 18 + i * 40);
                this.textBoxes[i].Multiline = true;
                this.textBoxes[i].Name = "Candidate nr" + i;
                this.textBoxes[i].Size = new System.Drawing.Size(200, 40);
                this.textBoxes[i].TabIndex = 0;

                for (int j = 0; j < Constants.BALLOTSIZE; j++)
                {
                    this.EAConnectButton.Enabled = false;
                    this.voteButtons[i].ElementAt(j).Location = new System.Drawing.Point(240 + j * 75, 17 + i * 40);
                    this.voteButtons[i].ElementAt(j).Name = "Candidate_nr;" + i + ";" + j;
                    this.voteButtons[i].ElementAt(j).Size = new System.Drawing.Size(70, 40);
                    this.voteButtons[i].ElementAt(j).TabIndex = 0;
                    this.voteButtons[i].ElementAt(j).Text = "NO";
                    this.voteButtons[i].ElementAt(j).UseVisualStyleBackColor = true;
                    this.voteButtons[i].ElementAt(j).Click += new System.EventHandler(voteButton_Click);
                    this.panel1.Controls.Add(voteButtons[i].ElementAt(j));
                }
            }


        }

        private void enableButtonsAfterLoadingConfiguration()
        {
            this.EAConnectButton.Enabled = true;
            this.ProxyConnectButton.Enabled = true;
        }

    }
}
