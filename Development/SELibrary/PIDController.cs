namespace IngameScript
{
    partial class Program
    {
        /*
         * Simple PID Controller by Khjin v1.0 10/29/2023
         */
        public class PIDController
        {
            private double Kp;     // Proportional gain
            private double Ki;     // Integral gain
            private double Kd;     // Derivative gain
            private double setpoint;
            private double integral;
            private double previousError;

            public PIDController(double kp, double ki, double kd)
            {
                Kp = kp;
                Ki = ki;
                Kd = kd;
                integral = 0;
                previousError = 0;
            }

            public double Update(double current)
            {
                double error = setpoint - current;

                // Proportional term
                double proportional = Kp * error;

                // Integral term
                integral += error;
                double integralTerm = Ki * integral;

                // Derivative term
                double derivative = Kd * (error - previousError);
                previousError = error;

                // Calculate the control output
                double output = proportional + integralTerm + derivative;

                return output;
            }

            public void SetPoint(double sp)
            {
                setpoint = sp;
                integral = 0;
                previousError = 0;
            }

            public void Reset()
            {
                integral = 0;
                previousError = 0;
            }
        }
    }
}
