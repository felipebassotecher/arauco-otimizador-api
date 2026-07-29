using System.Text.RegularExpressions;

namespace Techer.Common.Domain.DataSource
{
    public class DataSourceRequest
    {
        public int Skip { get; set; }
        public int Take { get; set; }
        public SortDescriptor[] Sorts { get; set; }
        public FilterDescriptor[] Filters { get; set; }
    }

    public class SortDescriptor
    {
        public string F { get; set; }
        public string D { get; set; }

        public string ToExpression()
        {
            return F + " " + D;
        }
    }

    public class FilterDescriptor
    {
        public string F { get; set; }
        public string O { get; set; }
        public object[] V { get; set; }

        public (string, object[]) ToExpression(int index)
        {
            string predicate = null;
            var values = new List<object>();

            string comparison = operators[O];

            switch (comparison)
            {
                case "between":
                    if (V[0] != null)
                    {
                        predicate = string.Format("{0} >= @{1}", F, index++);
                        values.Add(V[0]);
                    }

                    if (V.Length > 0 && V[1] != null)
                    {
                        if (!string.IsNullOrWhiteSpace(predicate))
                        {
                            predicate += " And ";
                        }
                        else
                        {
                            predicate = string.Empty;
                        }

                        predicate += string.Format("{0} <= @{1}", F, index);
                        values.Add(V[1]);
                    }
                    break;

                case "doesnotcontain":
                    predicate = string.Format("!{0}.{1}(@{2})", F, comparison, index);
                    values.Add(V[0].ToString());
                    break;

                case "StartsWith":
                case "EndsWith":
                case "Contains":
                    predicate = string.Format("{0}.{1}(@{2})", F, comparison, index);
                    values.Add(V[0].ToString());
                    break;

                case "pointintime":
                    string s = V[0].ToString();
                    Regex rex = new Regex(@"^([\d]+)([\w]{1})$");
                    Match m = rex.Match(s);
                    int v = 0;

                    if (m.Success && int.TryParse(m.Groups[1].Value, out v))
                    {
                        DateTime dateTime = DateTime.UtcNow;
                        switch (m.Groups[2].Value)
                        {
                            case "H":
                                dateTime = dateTime.AddHours(v * -1);
                                break;

                            case "D":
                                dateTime = dateTime.AddDays(v * -1);
                                break;

                            case "W":
                                dateTime = dateTime.AddDays(v * -7);
                                break;
                            case "M":
                                dateTime = dateTime.AddMonths(v * -1);
                                break;
                        }

                        predicate = string.Format("{0} >= @{1}", F, index);
                        values.Add(dateTime);
                    }
                    break;

                case "in":
                    predicate = string.Format("@{1}.Contains({0})", F, index);
                    //values.Add(new List<object>(V));
                    if (V[0] is int)
                    {
                        values.Add(new List<int>(V.Select(x => int.Parse(x.ToString()))));
                    }
                    else if (V[0] is long)
                    {
                        values.Add(new List<long>(V.Select(x => long.Parse(x.ToString()))));
                    }
                    else
                    {
                        values.Add(new List<string>(V.Select(x => x.ToString())));
                    }
                    break;

                default:
                    predicate = string.Format("{0} {1} @{2}", F, comparison, index);
                    values.Add(V[0]);
                    break;
            }

            return (predicate, values.ToArray());
        }

        private static readonly IDictionary<string, string> operators = new Dictionary<string, string>
        {
            {"eq", "="},
            {"neq", "!="},
            {"lt", "<"},
            {"lte", "<="},
            {"gt", ">"},
            {"gte", ">="},
            {"startswith", "StartsWith"},
            {"endswith", "EndsWith"},
            {"contains", "Contains"},
            {"doesnotcontain", "Contains"},
            {"bt", "between" },
            {"pt", "pointintime" },
            {"in", "in" }
        };

    }
}
