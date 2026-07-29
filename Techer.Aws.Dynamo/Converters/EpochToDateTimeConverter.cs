using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System;

namespace Techer.Aws.Dynamo.Converters
{
    public class EpochToDateTimeConverter : IPropertyConverter
    {
        public object? FromEntry(DynamoDBEntry entry)
        {
            if (entry is DynamoDBNull)
            {
                return null;
            }

            var entryString = entry.AsString();

            long numericValue;
            if (long.TryParse(entryString, out numericValue))
                return DateTimeOffset.FromUnixTimeSeconds(numericValue).DateTime;

            DateTime dateValue;
            if (DateTime.TryParse(entryString, out dateValue))
                return dateValue;

            throw new InvalidOperationException("Tipo de dados inválido");
        }

        public DynamoDBEntry ToEntry(object value)
        {
            var date = value as DateTime?;

            return new Primitive
            {
                Value = date.HasValue ? ((DateTimeOffset)date.Value).ToUnixTimeSeconds().ToString() : null
            };
        }
    }
}
