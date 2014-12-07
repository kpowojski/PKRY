﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Proxy
{
    class Constants
    {
        public const int LOG_INFO = 0;
        public const int LOG_MESSAGE = 1;
        public const int LOG_ERROR = 2;

        public const string ID = "ID";
        public const string PROXY_PORT = "proxyPort";
        public const string ELECTION_AUTHORITY_IP = "electionAuthorityIP";
        public const string ELECTION_AUTHORITY_PORT = "electionAuthorityPort";
        public const string CONFIGURATION_LOADED_FROM = "Configuration loaded from file: ";


        public const string LOCALHOST = "localhost";
        public const string CONNECTION_PASS = "Proxy connected with Election Authority correctly";
        public const string CONNECTION_FAILED = "Proxy could not connect to Election Authority";
        public const string CONNECTION_DISCONNECTED = "Proxy disconnected from Election Authority";
        public const string CONNECTION_DISCONNECTED_ERROR = "Error occured during disconnecting Proxy from Election Authority";

        public const string PATH_TO_CONFIG = @"Config\ElectionAuthority.xml";

        public const string SERVER_STARTED_CORRECTLY = "Proxy started working correctly";
        public const string SERVER_UNABLE_TO_START = "Proxy unable to start working";
        public const string UNKNOWN = "Unknown";
        public const string DISCONNECTED_NODE = "Someone has been disconnected";
    }
}