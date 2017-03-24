// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock.Model;
using Rock.Reporting;
using Rock.Web.UI.Controls;

namespace Rock.Field.Types
{
    /// <summary>
    /// Field used to save and display a pair of date values
    /// </summary>
    [Serializable]
    public class DateRangeFieldType : FieldType
    {
        #region Formatting

        /// <summary>
        /// Returns the field's current value(s)
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="value">Information about the value</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="condensed">Flag indicating if the value should be condensed (i.e. for use in a grid column)</param>
        /// <returns></returns>
        public override string FormatValue( System.Web.UI.Control parentControl, string value, System.Collections.Generic.Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            string formattedValue = DateRangePicker.FormatDelimitedValues( value ) ?? value;
            return base.FormatValue( parentControl, formattedValue, configurationValues, condensed );
        }

        #endregion

        #region Edit Control

        /// <summary>
        /// Creates the control(s) necessary for prompting user for a new value
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="id"></param>
        /// <returns>
        /// The control
        /// </returns>
        public override Control EditControl( System.Collections.Generic.Dictionary<string, ConfigurationValue> configurationValues, string id )
        {
            var dateRangePicker = new DateRangePicker { ID = id };
            return dateRangePicker;
        }

        /// <summary>
        /// Reads new values entered by the user for the field
        /// </summary>
        /// <param name="control">Parent control that controls were added to in the CreateEditControl() method</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override string GetEditValue( System.Web.UI.Control control, System.Collections.Generic.Dictionary<string, ConfigurationValue> configurationValues )
        {
            DateRangePicker editor = control as DateRangePicker;
            if ( editor != null )
            {
                return editor.DelimitedValues;
            }

            return null;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="value">The value.</param>
        public override void SetEditValue( System.Web.UI.Control control, System.Collections.Generic.Dictionary<string, ConfigurationValue> configurationValues, string value )
        {
            DateRangePicker editor = control as DateRangePicker;
            if ( editor != null )
            {
                editor.DelimitedValues = value;
            }
        }

        /// <summary>
        /// Tests the value to ensure that it is a valid value.  If not, message will indicate why
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="required">if set to <c>true</c> [required].</param>
        /// <param name="message">The message.</param>
        /// <returns>
        ///   <c>true</c> if the specified value is valid; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsValid( string value, bool required, out string message )
        {
            if ( !string.IsNullOrWhiteSpace( value ) )
            {
                DateTime result;
                string[] valuePair = value.Split( new char[] { ',' }, StringSplitOptions.None );
                if ( valuePair.Length <= 2 )
                {
                    foreach ( string v in valuePair )
                    {
                        if ( !string.IsNullOrWhiteSpace( v ) )
                        {
                            if ( !string.IsNullOrWhiteSpace( v ) )
                            {
                                if ( !DateTime.TryParse( v, out result ) )
                                {
                                    message = "The input provided contains invalid date values";
                                    return false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    message = "The input provided is not a valid date range.";
                    return false;
                }
            }

            return base.IsValid( value, required, out message );
        }

        #endregion

        #region Filter Control

        /// <summary>
        /// Gets the type of the filter comparison.
        /// </summary>
        /// <value>
        /// The type of the filter comparison.
        /// </value>
        public override ComparisonType FilterComparisonType
        {
            get { return ComparisonHelper.DateFilterComparisonTypes; }
        }

        /// <summary>
        /// Gets the filter value control.
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="required">if set to <c>true</c> [required].</param>
        /// <param name="filterMode">The filter mode.</param>
        /// <returns></returns>
        public override Control FilterValueControl( Dictionary<string, ConfigurationValue> configurationValues, string id, bool required, FilterMode filterMode )
        {
            var dateFiltersPanel = new Panel();
            dateFiltersPanel.ID = string.Format( "{0}_dtFilterControls", id );

            var datePickerPanel = new Panel();
            dateFiltersPanel.Controls.Add( datePickerPanel );

            //var datePicker = new DatePicker();
            //datePicker.ID = string.Format( "{0}_dtPicker", id );
            //datePicker.DisplayCurrentOption = true;
            //datePickerPanel.AddCssClass( "js-filter-control" );
            //datePickerPanel.Controls.Add( datePicker );

            var dateRangePicker = new DateRangePicker();
            dateRangePicker.ID = string.Format( "{0}_dtRangePicker", id );
            datePickerPanel.AddCssClass( "js-filter-control" );
            datePickerPanel.Controls.Add( dateRangePicker );

            //var slidingDateRangePicker = new SlidingDateRangePicker();
            //slidingDateRangePicker.ID = string.Format( "{0}_dtSlidingDateRange", id );
            //slidingDateRangePicker.AddCssClass( "js-filter-control-between" );
            //slidingDateRangePicker.Label = string.Empty;
            //slidingDateRangePicker.PreviewLocation = SlidingDateRangePicker.DateRangePreviewLocation.Right;
            //dateFiltersPanel.Controls.Add( slidingDateRangePicker );

            return dateFiltersPanel;
        }

        /// <summary>
        /// Determines whether this filter has a filter control
        /// </summary>
        /// <returns></returns>
        public override bool HasFilterControl()
        {
            return true;
        }

        /// <summary>
        /// Gets the filter value value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override string GetFilterValueValue( Control control, Dictionary<string, ConfigurationValue> configurationValues )
        {
            var dateFiltersPanel = control as Panel;
            var datePicker = dateFiltersPanel.ControlsOfTypeRecursive<DatePicker>().FirstOrDefault();
            var dateRangePicker = dateFiltersPanel.ControlsOfTypeRecursive<DateRangePicker>().FirstOrDefault();
            var slidingDateRangePicker = dateFiltersPanel.ControlsOfTypeRecursive<SlidingDateRangePicker>().FirstOrDefault();
            string datePickerValue = string.Empty;
            string dateRangePickerValue = string.Empty;
            string slidingDateRangePickerValue = string.Empty;
            if ( datePicker != null )
            {
                datePickerValue = this.GetEditValue( datePicker, configurationValues );
            }

            if ( dateRangePicker != null )
            {
                dateRangePickerValue = this.GetEditValue( dateRangePicker, configurationValues );
            }

            if ( slidingDateRangePicker != null )
            {
                slidingDateRangePickerValue = slidingDateRangePicker.DelimitedValues;
            }

            // use comma delimited
            return string.Format( "{0},{1}", datePickerValue, slidingDateRangePickerValue );
        }

        /// <summary>
        /// Sets the filter value value.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="value">The value.</param>
        public override void SetFilterValueValue( Control control, Dictionary<string, ConfigurationValue> configurationValues, string value )
        {
            var filterValues = value.Split( new string[] { "," }, StringSplitOptions.None );

            var dateFiltersPanel = control as Panel;
            var datePicker = dateFiltersPanel.ControlsOfTypeRecursive<DatePicker>().FirstOrDefault();
            if ( datePicker != null && filterValues.Length > 0 )
            {
                this.SetEditValue( datePicker, configurationValues, filterValues[0] );
            }

            var dateRangePicker = dateFiltersPanel.ControlsOfTypeRecursive<DateRangePicker>().FirstOrDefault();
            if ( dateRangePicker != null && filterValues.Length > 0 )
            {
                this.SetEditValue( dateRangePicker, configurationValues, filterValues[0] );
            }

            var slidingDateRangePicker = dateFiltersPanel.ControlsOfTypeRecursive<SlidingDateRangePicker>().FirstOrDefault();
            if ( slidingDateRangePicker != null && filterValues.Length > 1 )
            {
                slidingDateRangePicker.DelimitedValues = filterValues[1];
            }
        }

        /// <summary>
        /// Gets the filter format script.
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="title">The title.</param>
        /// <returns></returns>
        /// <remarks>
        /// This script must set a javascript variable named 'result' to a friendly string indicating value of filter controls
        /// a '$selectedContent' should be used to limit script to currently selected filter fields
        /// </remarks>
        public override string GetFilterFormatScript( Dictionary<string, ConfigurationValue> configurationValues, string title )
        {
            string titleJs = System.Web.HttpUtility.JavaScriptStringEncode( title );
            var format = "return Rock.reporting.formatFilterForDateField('{0}', $selectedContent);";
            return string.Format( format, titleJs );
        }

        /// <summary>
        /// Formats the filter values.
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="filterValues">The filter values.</param>
        /// <returns></returns>
        public override string FormatFilterValues( Dictionary<string, ConfigurationValue> configurationValues, List<string> filterValues )
        {
            if ( filterValues.Count >= 2 )
            {
                var filterValueValues = filterValues[1].Split( new string[] { "," }, StringSplitOptions.None );

                string comparisonValue = filterValues[0];
                if ( comparisonValue != "0" )
                {
                    ComparisonType comparisonType = comparisonValue.ConvertToEnum<ComparisonType>( ComparisonType.EqualTo );
                    if ( comparisonType == ComparisonType.Between && filterValueValues.Length > 1 )
                    {
                        var dateRangeText = SlidingDateRangePicker.FormatDelimitedValues( filterValueValues[1] );
                        return string.Format( "through '{0}'", dateRangeText );
                    }
                    else
                    {
                        List<string> filterValuesForFormat = new List<string>();
                        filterValuesForFormat.Add( filterValues[0] );
                        filterValuesForFormat.Add( filterValueValues[0] );
                        return base.FormatFilterValues( configurationValues, filterValuesForFormat );
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a filter expression for an entity property value.
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="filterValues">The filter values.</param>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <returns></returns>
        public override Expression PropertyFilterExpression( Dictionary<string, ConfigurationValue> configurationValues, List<string> filterValues, Expression parameterExpression, string propertyName, Type propertyType )
        {
            if ( filterValues.Count >= 2 )
            {
                // uses Tab Delimited since slidingDateRangePicker is | delimited
                var filterValueValues = filterValues[1].Split( new string[] { "," }, StringSplitOptions.None );

                // Parse for RelativeValue of DateTime (if specified)
                filterValueValues[0] = ParseRelativeValue( filterValueValues[0] );

                string comparisonValue = filterValues[0];
                if ( comparisonValue != "0" )
                {
                    ComparisonType comparisonType = comparisonValue.ConvertToEnum<ComparisonType>( ComparisonType.EqualTo );
                    MemberExpression propertyExpression = Expression.Property( parameterExpression, propertyName );
                    if ( comparisonType == ComparisonType.Between && filterValueValues.Length > 1 )
                    {
                        var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( filterValueValues[1] );
                        ConstantExpression constantExpressionLower = dateRange.Start.HasValue
                            ? Expression.Constant( dateRange.Start, typeof( DateTime ) )
                            : Expression.Constant( null );

                        ConstantExpression constantExpressionUpper = dateRange.End.HasValue
                            ? Expression.Constant( dateRange.End, typeof( DateTime ) )
                            : Expression.Constant( null );

                        return ComparisonHelper.ComparisonExpression( comparisonType, propertyExpression, constantExpressionLower, constantExpressionUpper );
                    }
                    else
                    {
                        var dateTime = filterValueValues[0].AsDateTime() ?? DateTime.MinValue;
                        ConstantExpression constantExpression = Expression.Constant( dateTime, typeof( DateTime ) );
                        return ComparisonHelper.ComparisonExpression( comparisonType, propertyExpression, constantExpression );
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a filter expression for an attribute value.
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="filterValues">The filter values.</param>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <returns></returns>
        public override Expression AttributeFilterExpression( Dictionary<string, ConfigurationValue> configurationValues, List<string> filterValues, ParameterExpression parameterExpression )
        {
            return PropertyFilterExpression( configurationValues, filterValues, parameterExpression, "ValueAsDateTime", typeof( DateTime? ) );
        }

        /// <summary>
        /// Gets the name of the attribute value field that should be bound to (Value, ValueAsDateTime, ValueAsBoolean, or ValueAsNumeric)
        /// </summary>
        /// <value>
        /// The name of the attribute value field.
        /// </value>
        public override string AttributeValueFieldName
        {
            get
            {
                return "ValueAsDateTime";
            }
        }

        /// <summary>
        /// Attributes the constant expression.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public override ConstantExpression AttributeConstantExpression( string value )
        {
            var dateTime = value.AsDateTime() ?? DateTime.MinValue;
            return Expression.Constant( dateTime, typeof( DateTime ) );
        }

        /// <summary>
        /// Gets the type of the attribute value field.
        /// </summary>
        /// <value>
        /// The type of the attribute value field.
        /// </value>
        public override Type AttributeValueFieldType
        {
            get
            {
                return typeof( DateTime? );
            }
        }

        /// <summary>
        /// Checks to see if value is for 'current' date and if so, adjusts the date value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual string ParseRelativeValue( string value )
        {
            if ( value.StartsWith( "CURRENT", StringComparison.OrdinalIgnoreCase ) )
            {
                DateTime currentDate = RockDateTime.Today;

                var valueParts = value.Split( ',' );

                if ( valueParts.Length > 1 )
                {
                    currentDate = currentDate.AddDays( valueParts[1].AsInteger() );
                }

                return currentDate.ToString( "o" );
            }

            return value;
        }

        #endregion
    }
}
