using System;

namespace IPod {

    public class DatabaseReadException : ApplicationException {

        public DatabaseReadException (Exception e, string msg, params object[] args) : base (String.Format (msg, args),
                                                                                             e) {
        }
        
        public DatabaseReadException (string msg, params object[] args) : base (String.Format (msg, args)) {
        }
    }
}
