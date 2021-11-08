using System;
using InterSystems.Data.IRISClient.ADO;
using InterSystems.Data.IRISClient.Gateway;
using InterSystems.EnsLib.PEX;

namespace test
{
    public class PEXService : BusinessService
    {
        /// <summary>
        /// Configuration item(s) to which to send file stream messages
        /// </summary>
        public string TargetConfigNames;

        /// <summary>
        /// Configuration item(s) to which to send file stream messages, parsed into string[]
        /// </summary>
        private string[] targets;

        /// <summary>
        /// Connection to InterSystems IRIS
        /// </summary>
        private IRIS iris;

        private int counter = 0;

        public override void OnInit()
        {
            LOGINFO("Initialization started");

            if (TargetConfigNames != null)
            {
                targets = TargetConfigNames.Split(',');
            }
            iris = GatewayContext.GetIRIS();

            LOGINFO("Initialized!");
        }

        public override object OnProcessInput(object messageInput)
        {
            bool atEnd = false;
            while (atEnd is false)
            {
                System.Threading.Thread.Sleep(1000);

                foreach (string target in targets)
                {
                    IRISObject request = (IRISObject)iris.ClassMethodObject("Ens.StringContainer", "%New", "test message from .NET #"+ counter);
                    SendRequestAsync(target, request);
                }
                counter++;
            }
            return null;
        }

        public override void OnTearDown()
        {
            iris.Close();
        }
    }
}
