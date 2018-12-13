using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ObjectDumper
{
    /// <summary>
    /// Interaction logic for WpfDialog.xaml
    /// </summary>
    public partial class ExportDialog : System.Windows.Window
    {
        private List<EnvDTE.Expression> Locals { get; set; }
        public ExportDialog(List<EnvDTE.Expression> locals)
        {
            InitializeComponent();
            Locals = locals;
            PopulateDropDown(locals);
        }

        
        private void PopulateDropDown(List<EnvDTE.Expression> locals)
        {
            try
            {
                Dispatcher.VerifyAccess();
                locals.ForEach(i => LocalDropDown.Items.Add(i.Name));
            }
            catch (Exception ex)
            {
                TypeInfo.Text = $"Exception of type {ex.GetType()} occured";
                OutPut.Text = ex.ToString();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private Stopwatch RunTimeTimer { get; } = new Stopwatch();

        private void LocalDropDown_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                RunTimeTimer.Start();
                Dispatcher.VerifyAccess();
                var dropDown = sender as ComboBox;
                var selectedLocal = Locals.FirstOrDefault(i => i.Name == dropDown.SelectedValue.ToString());

                var json = GenerateJson(selectedLocal);
                json = json.TrimEnd(',');
                OutPut.Text = "{" + json + "}";
                TypeInfo.Text = selectedLocal.Type.ToString();
            }
            catch (Exception ex)
            {
                TypeInfo.Text = $"Exception of type {ex.GetType()} occured";
                OutPut.Text = ex.ToString();
            }
            RunTimeTimer.Stop();
            RunTimeTimer.Reset();
        }

        private Regex PartOfCollection { get; } = new Regex(@"\[\d+\]");
        private string GenerateJson(EnvDTE.Expression currentExpression)
        {
            if(RunTimeTimer.ElapsedMilliseconds > 10000)
            {
                throw new TimeoutException("Timeout while generating JSON.");
            }
            Dispatcher.VerifyAccess();
            if (ExpressionIsDictionary(currentExpression))
            {
                var ret = string.Empty;
                foreach (EnvDTE.Expression dicSubExpression in currentExpression.DataMembers)
                {
                    if (PartOfCollection.IsMatch(dicSubExpression.Name))
                    {
                        string key = null;
                        string value = null;
                        foreach (EnvDTE.Expression dicCollectionExpression in dicSubExpression.DataMembers)
                        {
                            if (dicCollectionExpression.Name == "key") 
                            {
                                if (ExpressionIsValue(dicCollectionExpression) || ExpressionIsListOrArray(dicCollectionExpression))
                                {
                                    key = GenerateJson(dicCollectionExpression);
                                }
                                else
                                {
                                    key = $"{{{GenerateJson(dicCollectionExpression)}}}";
                                }
                               
                            }
                            if(dicCollectionExpression.Name == "value")
                            {
                                if (ExpressionIsValue(dicCollectionExpression) || ExpressionIsListOrArray(dicCollectionExpression))
                                {
                                    value = GenerateJson(dicCollectionExpression);
                                }
                                else
                                {
                                    value = $"{{{GenerateJson(dicCollectionExpression)}}}";
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

                foreach (EnvDTE.Expression ex in currentExpression.DataMembers)
                {
                    if (PartOfCollection.IsMatch(ex.Name))
                    {
                        if (ExpressionIsValue(ex) || ExpressionIsListOrArray(ex))
                        {
                            ret += GenerateJson(ex);
                        }
                        else
                        {
                            ret += $"{{{GenerateJson(ex)}}}";
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
                foreach (EnvDTE.Expression subExpression in currentExpression.DataMembers)
                {
                    if (subExpression.Value == "null")
                    {
                        ret += $"\"{subExpression.Name}\":null";
                    }
                    else if (ExpressionIsValue(subExpression) || ExpressionIsListOrArray(subExpression))
                    {
                        ret += $"\"{subExpression.Name}\":{GenerateJson(subExpression)}";
                    }

                    else
                    {
                        ret += $"\"{subExpression.Name}\":{{{GenerateJson(subExpression)}}}";
                    }
                    ret += ",";
                }
                ret = ret.TrimEnd(',');
                return ret;
            }

        }

        private string GetJsonRepresentationofValue(EnvDTE.Expression exp)
        {
            Dispatcher.VerifyAccess();
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

        private bool ExpressionIsListOrArray(EnvDTE.Expression exp)
        {
            Dispatcher.VerifyAccess();
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

        private bool ExpressionIsDictionary(EnvDTE.Expression exp)
        {
            Dispatcher.VerifyAccess();
            if (exp.Type.StartsWith("System.Collections.Generic.Dictionary"))
            {
                return true;
            }          
            return false;
        }

        private bool ExpressionIsValue(EnvDTE.Expression exp)
        {
            Dispatcher.VerifyAccess();
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
