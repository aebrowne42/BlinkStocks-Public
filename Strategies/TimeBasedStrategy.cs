#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public enum SessionDirection
    {
        Long,
        Short
    }

    public class MorningSessionStrategy : Strategy
    {
        [NinjaScriptProperty]
        [Display(Name = "Direction", Description = "Select Long or Short trade execution", Order = 1, GroupName = "1. Session Parameters")]
        public SessionDirection Direction { get; set; }

        // Fixed: Changed to DateTime to properly bind to the UI Time Picker
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Start Time", Description = "Time to enter position", Order = 2, GroupName = "1. Session Parameters")]
        public DateTime StartTime { get; set; }

        // Fixed: Changed to DateTime to properly bind to the UI Time Picker
        [NinjaScriptProperty]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "End Time", Description = "Time to exit position", Order = 3, GroupName = "1. Session Parameters")]
        public DateTime EndTime { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Contract Quantity", Description = "Number of contracts to trade", Order = 1, GroupName = "2. Risk Management")]
        public int Quantity { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Max Trade Loss ($)", Description = "Maximum drawdown allowed per trade in dollars (0 to disable)", Order = 2, GroupName = "2. Risk Management")]
        public double MaxTradeLossDollars { get; set; }

        // --- Day of Week Checkboxes ---
        [NinjaScriptProperty]
        [Display(Name = "Monday", Description = "Enable trading on Mondays", Order = 1, GroupName = "3. Days Filter")]
        public bool TradeMonday { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Tuesday", Description = "Enable trading on Tuesdays", Order = 2, GroupName = "3. Days Filter")]
        public bool TradeTuesday { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Wednesday", Description = "Enable trading on Wednesdays", Order = 3, GroupName = "3. Days Filter")]
        public bool TradeWednesday { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Thursday", Description = "Enable trading on Thursdays", Order = 4, GroupName = "3. Days Filter")]
        public bool TradeThursday { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Friday", Description = "Enable trading on Fridays", Order = 5, GroupName = "3. Days Filter")]
        public bool TradeFriday { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Enters Long or Short at a specified start time and exits at an end time with max drawdown protection and day filters.";
                Name = "MorningSessionStrategy";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsInstantiatedOnEachOptimizationIteration = true;

                // Default Parameters
                Direction = SessionDirection.Long;
                
                // Initialized as DateTime for the UI Picker
                StartTime = DateTime.Parse("07:00:00", System.Globalization.CultureInfo.InvariantCulture);
                EndTime = DateTime.Parse("10:00:00", System.Globalization.CultureInfo.InvariantCulture);
                
                Quantity = 1;                    
                MaxTradeLossDollars = 500;       

                // Default Days Active
                TradeMonday = true;
                TradeTuesday = true;
                TradeWednesday = true;
                TradeThursday = true;
                TradeFriday = true;
            }
            else if (State == State.Configure)
            {
                if (MaxTradeLossDollars > 0)
                {
                    SetStopLoss(CalculationMode.Currency, MaxTradeLossDollars);
                }
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1) return;

            int currentTime = ToTime(Time[0]);
            int previousTime = ToTime(Time[1]);
            
            // Fixed: Convert the UI DateTime to NinjaTrader's HHMMSS integer format on the fly
            int targetStartTime = ToTime(StartTime);
            int targetEndTime = ToTime(EndTime);

            // Check if current bar falls on an enabled trading day
            bool isDayAllowed = (Time[0].DayOfWeek == DayOfWeek.Monday && TradeMonday) ||
                               (Time[0].DayOfWeek == DayOfWeek.Tuesday && TradeTuesday) ||
                               (Time[0].DayOfWeek == DayOfWeek.Wednesday && TradeWednesday) ||
                               (Time[0].DayOfWeek == DayOfWeek.Thursday && TradeThursday) ||
                               (Time[0].DayOfWeek == DayOfWeek.Friday && TradeFriday);

            // Entry Condition: Time crosses targetStartTime, day is enabled, and position is Flat
            if (Position.MarketPosition == MarketPosition.Flat && isDayAllowed)
            {
                if (currentTime >= targetStartTime && previousTime < targetStartTime)
                {
                    if (Direction == SessionDirection.Long)
                        EnterLong(Quantity, "SessionTrade");
                    else if (Direction == SessionDirection.Short)
                        EnterShort(Quantity, "SessionTrade");
                }
            }

            // Exit Condition: Time reaches or exceeds targetEndTime and a position is open
            if (Position.MarketPosition != MarketPosition.Flat)
            {
                if (currentTime >= targetEndTime)
                {
                    if (Position.MarketPosition == MarketPosition.Long)
                        ExitLong("SessionTrade");
                    else if (Position.MarketPosition == MarketPosition.Short)
                        ExitShort("SessionTrade");
                }
            }
        }
    }
}