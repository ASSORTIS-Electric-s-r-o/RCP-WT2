using System;

namespace RCP_WT1.SerialComm
{
    // ==========================================
    // Logika dávkování váhy
    // ==========================================
    public class WeightDosing
    {
        // ==========================================
        // Veřejné proměnné
        // ==========================================

        public bool Activate { get; private set; } = false;

        public bool Hold { get; private set; } = false;

        public bool ResetRequest { get; private set; } = false;

        public double StartValue { get; private set; } = 0.0;

        public double LockValue { get; private set; } = 0.0;

        public double Difference { get; private set; } = 0.0;


        // ==========================================
        // Pomocné proměnné
        // ==========================================

        private bool PrevActivate = false;

        private bool PrevHold = false;


        // ==========================================
        // Start vážení
        // ==========================================
        public void Start()
        {
            Activate = true;
            ResetRequest = false;
        }


        // ==========================================
        // Stop vážení
        // ==========================================
        public void Stop()
        {
            Activate = false;
            Hold = false;
            ResetRequest = false;
            Difference = 0;
        }


        // ==========================================
        // Reset vážení
        // ==========================================
        public void Reset()
        {
            ResetRequest = true;
        }


        // ==========================================
        // HOLD ON/OFF
        // ==========================================
        public void SetHold(bool state)
        {
            Hold = state;
        }


        // ==========================================
        // Aktualizace hodnoty z váhy
        // ==========================================
        public void Update(double currentWeight)
        {
            bool risingActivate = Activate && !PrevActivate;
            bool risingHold = Hold && !PrevHold;
            bool fallingHold = !Hold && PrevHold;

            PrevActivate = Activate;
            PrevHold = Hold;


            // ==========================================
            // Start nebo reset
            // ==========================================

            if (risingActivate || ResetRequest)
            {
                StartValue = currentWeight;
                LockValue = currentWeight;
                Difference = 0;
                ResetRequest = false;
            }


            // ==========================================
            // Váha není aktivní
            // ==========================================

            if (!Activate)
            {
                LockValue = currentWeight;
                Hold = false;
                Difference = 0;
                return;
            }


            // ==========================================
            // HOLD ON
            // ==========================================

            if (risingHold)
            {
                LockValue = currentWeight - Difference;
            }


            // ==========================================
            // HOLD OFF
            // ==========================================

            if (fallingHold)
            {
                LockValue = currentWeight - Difference;
            }


            // ==========================================
            // Normální výpočet
            // ==========================================

            if (!Hold)
            {
                Difference = currentWeight - LockValue;
            }
        }
    }
}