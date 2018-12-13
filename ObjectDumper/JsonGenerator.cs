using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using EnvDTE;

namespace ObjectDumper
{
    internal class JsonGenerator
    {       
        private Stopwatch RuntimeTimer { get; } = new Stopwatch();
        public int TimeOutInSeconds { get; set; } = 10;
        public JsonGenerator()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        }
        private Regex PartOfCollection { get; } = new Regex(@"\[\d+\]");

        public string GenerateJson(Expression expression)
        {
            RuntimeTimer.Start();
            var result = GenerateJsonRecurse(expression);
            RuntimeTimer.Stop();
            RuntimeTimer.Reset();
            result = result.TrimEnd(',');
            result = "{" + result + "}";
            return result;
        }

        private string GenerateJsonRecurse(Expression currentExpression)
        {
            if (RuntimeTimer.ElapsedMilliseconds > (TimeOutInSeconds*1000))
            {
                throw new TimeoutException("Timeout while generating JSON.");
            }
            if (ExpressionIsDictionary(currentExpression))
            {
                var ret = string.Empty;
                foreach (Expression dicSubExpression in currentExpression.DataMembers)
                {
                    if (PartOfCollection.IsMatch(dicSubExpression.Name))
                    {
                        string key = null;
                        string value = null;
                        foreach (Expression dicCollectionExpression in dicSubExpression.DataMembers)
                        {
                            if (dicCollectionExpression.Name == "key")
                            {
                                if (ExpressionIsValue(dicCollectionExpression) || ExpressionIsListOrArray(dicCollectionExpression))
                                {
                                    key = GenerateJsonRecurse(dicCollectionExpression);
                                }
                                else
                                {
                                    key = $"{{{GenerateJsonRecurse(dicCollectionExpression)}}}";
                                }

                            }
                            if (dicCollectionExpression.Name == "value")
                            {
                                if (ExpressionIsValue(dicCollectionExpression) || ExpressionIsListOrArray(dicCollectionExpression))
                                {
                                    value = GenerateJsonRecurse(dicCollectionExpression);
                                }
                                else
                                {
                                    value = $"{{{GenerateJsonRecurse(dicCollectionExpression)}}}";
                                }
                            }

                        }
                        ret += $"{key}:{value},";
                    }
                }
                ret = ret.TrimEnd(',');
                return ret;
            }


            if (ExpressionIsValue(currentExpression))
            {
                return $"{GetJsonRepresentationofValue(currentExpression)}";
            }
            else if (ExpressionIsListOrArray(currentExpression))
            {
                var ret = string.Empty;
                ret += "[";

                foreach (Expression ex in currentExpression.DataMembers)
                {
                    if (PartOfCollection.IsMatch(ex.Name))
                    {
                        if (ExpressionIsValue(ex) || ExpressionIsListOrArray(ex))
                        {
                            ret += GenerateJsonRecurse(ex);
                        }
                        else
                        {
                            ret += $"{{{GenerateJsonRecurse(ex)}}}";
                        }
                        ret += ",";
                    }
                }
                ret = ret.TrimEnd(',');
                ret += $"]";
                return ret;
            }
            else
            {
                var ret = string.Empty;
                foreach (Expression subExpression in currentExpression.DataMembers)
                {
                    if (subExpression.Value == "null")
                    {
                        ret += $"\"{subExpression.Name}\":null";
                    }
                    else if (ExpressionIsValue(subExpression) || ExpressionIsListOrArray(subExpression))
                    {
                        ret += $"\"{subExpression.Name}\":{GenerateJsonRecurse(subExpression)}";
                    }

                    else
                    {
                        ret += $"\"{subExpression.Name}\":{{{GenerateJsonRecurse(subExpression)}}}";
                    }
                    ret += ",";
                }
                ret = ret.TrimEnd(',');
                return ret;
            }

        }

        private string GetJsonRepresentationofValue(Expression exp)
        {
            
            switch (exp.Type.Trim('?'))
            {
                case "System.DateTime":
                    return $"\"{exp.Value.Replace("{", "").Replace("}", "")}\"";
                case "int":
                case "string":
                case "bool":
                case "double":
                case "float":
                case "decimal":
                case "long":
                    return exp.Value;
                case "char":
                    return $"\"{exp.Value.Substring(exp.Value.IndexOf("'") + 1, 1)}\"";
                default:
                    return string.Empty;
            }
        }

        private bool ExpressionIsListOrArray(Expression exp)
        {
            if (exp.Type.StartsWith("System.Collections.Generic.List"))
            {
                return true;
            }
            if (exp.Type.EndsWith("[]"))
            {
                return true;
            }
            return false;
        }

        private bool ExpressionIsDictionary(Expression exp)
        {
            if (exp.Type.StartsWith("System.Collections.Generic.Dictionary"))
            {
                return true;
            }
            return false;
        }

        private bool ExpressionIsValue(Expression exp)
        {
            switch (exp.Type.Trim('?'))
            {
                case "string":
                case "System.DateTime":
                case "int":
                case "char":
                case "bool":
                case "double":
                case "float":
                case "decimal":
                case "long":
                    return true;
                default:
                    return false;
            }
        }

    }
}
