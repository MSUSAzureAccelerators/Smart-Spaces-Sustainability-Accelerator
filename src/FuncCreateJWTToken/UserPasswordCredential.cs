namespace FuncCreateJWTToken
{
    internal class UserPasswordCredential
    {
        private string username;
        private string password;

        public UserPasswordCredential(string username, string password)
        {
            this.username = username;
            this.password = password;
        }
    }
}