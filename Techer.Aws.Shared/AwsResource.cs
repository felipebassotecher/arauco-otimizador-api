using Techer.Common.Domain.Enums;

namespace Techer.Aws.Shared
{
    public class AwsResource<T>
    {
        public string Region { get; set; }
        public T Production { get; set; }
        public T Testing { get; set; }
        public T Development { get; set; }

        public AwsResource()
        {
        }

        public AwsResource(string region, T value)
        {
            Region = region;
            Development = value;
            Testing = value;
            Production = value;
        }

        public AwsResource(string region, T dev, T test, T prod)
        {
            Region = region;
            Development = dev;
            Testing = test;
            Production = prod;
        }

        public T Get(EnvironmentEnum env)
        {
            T value = default;

            switch (env)
            {
                case EnvironmentEnum.Dev:
                    value = Development;
                    break;

                case EnvironmentEnum.Test:
                    value = Testing;
                    break;

                case EnvironmentEnum.Prod:
                    value = Production;
                    break;
            }

            return value;
        }
    }
}
