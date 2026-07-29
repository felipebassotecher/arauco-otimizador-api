using System.Text.RegularExpressions;
using Techer.Common.Extensions;

namespace Techer.Common.Validation
{
    public static class PessoaValidator
    {
        public static bool ValidarNome(string n)
        {
            var regex = new Regex("^([a-zA-ZéúíóáÉÚÍÓÁèùìòàÈÙÌÒÀõãñÕÃÑêûîôâÊÛÎÔÂçÇ]+[ ]+)+(([a-zA-ZéúíóáÉÚÍÓÁèùìòàÈÙÌÒÀõãñÕÃÑêûîôâÊÛÎÔÂçÇ]+))[ ]*$");
            return regex.IsMatch(n);
        }

        public static bool ValidarCpf(string cpf)
        {
            int i;
            string s = cpf.GetNumbers();

            if (s == "11111111111" ||
                s == "22222222222" ||
                s == "33333333333" ||
                s == "44444444444" ||
                s == "55555555555" ||
                s == "66666666666" ||
                s == "77777777777" ||
                s == "88888888888" ||
                s == "99999999999" ||
                s == "00000000000" ||
                s.Length != 11)
            {

                return false;
            }

            string c = s.Substring(0, 9);
            string dv = s.Substring(9, 2);
            int d1 = 0;

            for (i = 0; i < 9; i++)
                d1 += int.Parse(c[i].ToString()) * (10 - i);

            if (d1 == 0)
            {
                return false;
            }

            d1 = 11 - d1 % 11;
            if (d1 > 9) d1 = 0;

            if (int.Parse(dv[0].ToString()) != d1)
            {
                return false;
            }

            d1 *= 2;
            for (i = 0; i < 9; i++) d1 += int.Parse(c[i].ToString()) * (11 - i);

            d1 = 11 - d1 % 11;
            if (d1 > 9) d1 = 0;

            if (int.Parse(dv[1].ToString()) != d1)
            {
                return false;
            }

            return true;
        }

        public static bool ValidarCnpj(string s)
        {
            string cnpj = s.GetNumbers();

            if (cnpj.Length != 14)
                return false;

            // Caso coloque todos os numeros iguais
            switch (cnpj)
            {
                case "11111111111111":
                    return false;
                case "00000000000000":
                    return false;
                case "22222222222222":
                    return false;
                case "33333333333333":
                    return false;
                case "44444444444444":
                    return false;
                case "55555555555555":
                    return false;
                case "66666666666666":
                    return false;
                case "77777777777777":
                    return false;
                case "88888888888888":
                    return false;
                case "99999999999999":
                    return false;
            }

            string l, inx, dig;
            int s1, s2, i, d1, d2, v, m1, m2;

            inx = cnpj.Substring(12, 2);
            cnpj = cnpj.Substring(0, 12);
            s1 = 0;
            s2 = 0;
            m2 = 2;

            for (i = 11; i >= 0; i--)
            {
                l = cnpj.Substring(i, 1);
                v = Convert.ToInt16(l);
                m1 = m2;
                m2 = m2 < 9 ? m2 + 1 : 2;
                s1 += v * m1;
                s2 += v * m2;
            }

            s1 %= 11;
            d1 = s1 < 2 ? 0 : 11 - s1;
            s2 = (s2 + 2 * d1) % 11;
            d2 = s2 < 2 ? 0 : 11 - s2;
            dig = d1.ToString() + d2.ToString();

            return inx == dig;
        }

        public static string FormatarCnpjCpf(this string s)
        {
            if (ValidarCpf(s.Substring(3)))
                return s.Substring(3).ToFormat("###.###.###-##");
            else
                return s.ToFormat("###.###.###/####-##");
        }

    }
}
