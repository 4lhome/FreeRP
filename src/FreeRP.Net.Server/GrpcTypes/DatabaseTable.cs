using System.Text.Json.Nodes;

namespace FreeRP.GrpcService.Database
{
    public partial class DatabaseTable
    {
        private readonly string[] _tableIds = ["Id", "id"];
        private readonly JsonObject _jsonObj;
        public bool HasId { get; set; }

        public DatabaseTable(Google.Protobuf.ByteString bytes, string tableId)
        {
            try
            {
                var jn = JsonNode.Parse(bytes.Span);
                if (jn is not null)
                {
                    _jsonObj = jn.AsObject();
                    TableId = tableId;
                    bool findId = false;

                    foreach (var item in _jsonObj)
                    {
                        var f = GetField(item);
                        if (f is not null)
                        {
                            Fields.Add(f);
                            if (findId == false && f.DataType == DatabaseDataType.FieldString)
                            {
                                foreach (var id in _tableIds)
                                    if ($"{tableId}{id}" == item.Key)
                                    {
                                        f.IsId = true;
                                        findId = true;
                                        break;
                                    }

                                if (f.IsId == false)
                                {
                                    foreach (var id in _tableIds)
                                        if (id == item.Key)
                                        {
                                            f.IsId = true;
                                            findId = true;
                                            break;
                                        }
                                }
                            }
                        }
                    }
                }
                throw new Net.Server.Exceptions.ErrorTypeException(Core.ErrorType.ErrorUnknown, "");
            }
            catch (Exception ex)
            {
                throw new Net.Server.Exceptions.ErrorTypeException(Core.ErrorType.ErrorUnknown, ex.Message);
            }
        }

        private static DatabaseTableField? GetField(KeyValuePair<string, JsonNode?> json)
        {
            if (json.Value is not null)
            {
                switch (json.Value.GetValueKind())
                {
                    case System.Text.Json.JsonValueKind.Array:
                        return new() { FieldId = json.Key, DataType = DatabaseDataType.FieldArray };
                    case System.Text.Json.JsonValueKind.True:
                    case System.Text.Json.JsonValueKind.False:
                        return new() { FieldId = json.Key, DataType = DatabaseDataType.FieldBoolean };
                    case System.Text.Json.JsonValueKind.Null:
                        return new() { FieldId = json.Key, DataType = DatabaseDataType.FieldNull };
                    case System.Text.Json.JsonValueKind.Number:
                        return new() { FieldId = json.Key, DataType = DatabaseDataType.FieldNumber };
                    case System.Text.Json.JsonValueKind.String:
                        return new() { FieldId = json.Key, DataType = DatabaseDataType.FieldString };
                    case System.Text.Json.JsonValueKind.Object:
                        {
                            DatabaseTableField f = new() { FieldId = json.Key, DataType = DatabaseDataType.FieldObject };
                            foreach (var o in json.Value.AsObject())
                            {
                                f.Fields.Add(GetField(o));
                            }
                            return f;
                        }

                }
            }

            return null;
        }
    }
}
