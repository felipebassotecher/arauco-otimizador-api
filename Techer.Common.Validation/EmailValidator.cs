namespace Techer.Common.Validation
{
    public static class EmailValidator
    {
        public static bool IsValid(string e)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(e);

                return addr.Address == e;
            }
            catch
            {
                return false;
            }
        }
    }
}
