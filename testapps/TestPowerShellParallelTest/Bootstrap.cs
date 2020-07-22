using Amazon.Lambda.PowerShellHost;

namespace TestPowerShellParallelTest
{
    public class Bootstrap : PowerShellFunctionHost
    {
        public Bootstrap() : base("TestPowerShellParallelTest.ps1")
        {
        }
    }
}
