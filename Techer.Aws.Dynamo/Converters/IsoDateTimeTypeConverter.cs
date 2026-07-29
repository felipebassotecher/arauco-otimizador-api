using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System;

namespace Techer.Aws.Dynamo.Converters
{
    public class IsoDateTimeTypeConverter : IPropertyConverter
    {
        public object FromEntry(DynamoDBEntry entry)
        {
            if (entry is DynamoDBNull)
                return null;

            return DateTime.Parse(entry.AsString());
        }

        public DynamoDBEntry ToEntry(object value)
        {
            if (!(value is DateTime))
            {
                throw new Exception("Field is not a DateTime");
            }

            if (value == null)
            {
                return DynamoDBNull.Null;
            }

            return new Primitive
            {
                Value = ((DateTime)value).ToString("s")
            };
        }
    }
}
