using PhoneNumbers;
using System.Text.RegularExpressions;
using Techer.Common.Extensions;

namespace Techer.Common.Validation
{
    public static class TelefoneValidator
    {
        public static bool IsMobile(string numero)
        {
            return numero.Length == 9 && numero[0] == '9';
        }

        public static bool ValidarDdd(string ddd)
        {
            bool valido = false;

            int n = 0;
            if (int.TryParse(ddd, out n))
                valido = n >= 11 && n <= 99;

            return valido;
        }

        public static bool ValidarCelular(string t)
        {
            bool valido = false;

            t = t.GetNumbers();

            if (!string.IsNullOrWhiteSpace(t) && t.Length >= 10 && t.Length <= 11)
            {
                var ddd = t.Substring(0, 2);
                var celular = t.Substring(2);

                if (ValidarDdd(ddd))
                {
                    valido = celular.Length == 9 && celular.Substring(0, 1) == "9";
                }
            }

            return valido;
        }

        public static bool ValidarNumeroWhatsApp(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                return false;

            var regex = new Regex(@"^\d{2}\s?\d{2,5}\s?\d{4,11}$");

            if (regex.IsMatch(numero))
            {
                var phoneNumberUtil = PhoneNumberUtil.GetInstance();
                var numeroCelular = phoneNumberUtil.Parse(numero, "BR");

                // Celular válido
                if (phoneNumberUtil.IsValidNumber(numeroCelular) && phoneNumberUtil.GetNumberType(numeroCelular) == PhoneNumberType.MOBILE)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }


        public static bool ValidarFixo(string t)
        {
            bool valido = false;

            t = t.GetNumbers();

            if (!string.IsNullOrWhiteSpace(t) && t.Length >= 10 && t.Length <= 11)
            {
                var ddd = t.Substring(0, 2);
                var numero = t.Substring(2);

                if (ValidarDdd(ddd))
                {
                    var array = new char[] { '2', '3', '4', '5' };

                    valido = numero.Length == 8 && array.Contains(numero[0]);
                }
            }

            return valido;
        }

        public static bool IsValid(string numero)
        {
            bool isValid = false;

            try
            {
                var num = "+" + numero.GetNumbers();
                var phoneNumberUtil = PhoneNumbers.PhoneNumberUtil.GetInstance();

                var phone = phoneNumberUtil.Parse(num, null);

                isValid = phoneNumberUtil.IsValidNumber(phone);
            }
            catch (Exception)
            {

            }


            return isValid;
        }

        public static string FormatarTelefone(this string t)
        {
            if (!string.IsNullOrWhiteSpace(t))
            {
                t = t.Trim().GetNumbers();

                var phoneNumberUtil = PhoneNumbers.PhoneNumberUtil.GetInstance();
                var num = "+" + t.GetNumbers();

                var phone = phoneNumberUtil.Parse(num, null);

                t = phoneNumberUtil.Format(phone, PhoneNumbers.PhoneNumberFormat.E164);
            }

            return t;
        }
    }
}
