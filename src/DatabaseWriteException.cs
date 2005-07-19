
using System;

namespace IPod {

    public class DatabaseWriteException : ApplicationException {

        public DatabaseWriteException (Exception e, string msg, params object[] args) :
            base (String.Format (msg, args), e) {
        }
        
        public DatabaseWriteException (string msg, params object[] args) : base (String.Format (msg, args)) {
        }
    }
}
