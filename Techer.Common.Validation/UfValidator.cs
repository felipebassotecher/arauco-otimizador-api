namespace Techer.Common.Validation
{
    public static class UfValidator
    {
        public static bool IsValid(string uf)
        {
            bool valido = false;

            if (uf != null)
                uf = uf.Trim().ToUpper();

            switch (uf)
            {
                case "AC":
                case "AL":
                case "AM":
                case "AP":
                case "BA":
                case "CE":
                case "DF":
                case "ES":
                case "GO":
                case "MA":
                case "MG":
                case "MS":
                case "MT":
                case "PA":
                case "PB":
                case "PE":
                case "PI":
                case "PR":
                case "RJ":
                case "RN":
                case "RO":
                case "RR":
                case "RS":
                case "SC":
                case "SE":
                case "SP":
                case "TO":
                    valido = true;
                    break;
            }

            return valido;
        }
    }
}
