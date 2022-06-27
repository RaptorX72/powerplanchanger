using System.ServiceProcess;

namespace PowerplanChanger {
	static class Program {

		static void Main() {
			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[] { new PowerSetter() };
			ServiceBase.Run(ServicesToRun);
		}
	}
}
