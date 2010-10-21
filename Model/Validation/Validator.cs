using System.Collections;
using System.Windows;
using System.Windows.Data;

namespace AXToolbox.Model.Validation
{
    public static class Validator
    {
        public static bool IsValid(DependencyObject obj)
        {
            // Validate all the bindings on the parent
            LocalValueEnumerator localValues = obj.GetLocalValueEnumerator();
            while (localValues.MoveNext())
            {
                LocalValueEntry entry = localValues.Current;
                if (BindingOperations.IsDataBound(obj, entry.Property))
                {
                    Binding binding = BindingOperations.GetBinding(obj, entry.Property);
                    if (binding.ValidationRules.Count > 0)
                    {
                        BindingExpression expression = BindingOperations.GetBindingExpression(obj, entry.Property);
                        expression.UpdateSource();

                        if (expression.HasError)
                        {
                            return false; //early exit
                        }
                    }
                }
            }

            // Validate all the bindings on the children
            IEnumerable children = LogicalTreeHelper.GetChildren(obj);
            foreach (object child in children)
            {
                if (child is DependencyObject)
                {
                    if (!IsValid((DependencyObject)child))
                    {
                        return false; //early exit
                    }
                }
            }

            return true;
        }
    }
}
