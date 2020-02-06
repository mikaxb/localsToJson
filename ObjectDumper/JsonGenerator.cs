using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;

namespace ObjectDumper
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "Done in constructor.")]
    internal class JsonGenerator
    {
        public JsonGenerator()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        }

        public int TimeOutInSeconds { get; } = 10;
        private Regex PartOfCollection { get; } = new Regex(@"\[\d+\]");
        private Stopwatch RuntimeTimer { get; } = new Stopwatch();

        public string GenerateJson(Expression expression)
        {
            try
            {
                RuntimeTimer.Start();
                var result = GenerateJsonRecurse(expression);
                RuntimeTimer.Stop();
                RuntimeTimer.Reset();
                return result;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Could not generate JSON due to {ex.GetType().Name}: {ex.Message}.{Environment.NewLine}Sorry, this extension cannot handle everything.{Environment.NewLine}Try a smaller, less complex object.");
            }
            return string.Empty;
        }

        private bool ExpressionIsDictionary(Expression exp)
        {
            if (exp.Type.StartsWith("System.Collections.Generic.Dictionary"))
            {
                return true;
            }
            return false;
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

      
        private string GenerateJsonRecurse(Expression currentExpression)
        {          
            if (RuntimeTimer.ElapsedMilliseconds > (TimeOutInSeconds * 1000))
            {
                throw new TimeoutException("Timeout while generating JSON.");
            }
            if (ExpressionIsDictionary(currentExpression))
            {
                var values = new StringBuilder();
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
                                key = GenerateJsonRecurse(dicCollectionExpression);
                            }
                            if (dicCollectionExpression.Name == "value")
                            {
                                value = GenerateJsonRecurse(dicCollectionExpression);
                            }
                        }
                        values.Append($"{key}:{value},");
                    }
                }
                return $"{{{values.ToString().TrimEnd(',')}}}";
            }

            if (ExpressionIsValue(currentExpression))
            {
                return $"{GetJsonRepresentationofValue(currentExpression)}";
            }
            else if (ExpressionIsListOrArray(currentExpression))
            {
                var values = new StringBuilder();
                foreach (Expression ex in currentExpression.DataMembers)
                {
                    if (PartOfCollection.IsMatch(ex.Name))
                    {
                        values.Append(GenerateJsonRecurse(ex) + ",");
                    }
                }
                return $"[{values.ToString().TrimEnd(',')}]";
            }
            else
            {
                var values = new StringBuilder();
                foreach (Expression subExpression in currentExpression.DataMembers)
                {
                    if (subExpression.Value == "null")
                    {
                        values.Append($"\"{subExpression.Name}\":null");
                    }
                    else if (ExpressionIsValue(subExpression) || ExpressionIsListOrArray(subExpression))
                    {
                        values.Append($"\"{subExpression.Name}\":{GenerateJsonRecurse(subExpression)}");
                    }
                    else
                    {
                        values.Append($"\"{subExpression.Name}\":{GenerateJsonRecurse(subExpression)}");
                    }
                    values.Append(",");
                }
                return $"{{{values.ToString().TrimEnd(',')}}}";
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
    }
}