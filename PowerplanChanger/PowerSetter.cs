using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.IO;
using System.Xml;
using System.Timers;

namespace PowerplanChanger {
	[RunInstaller(true)]
	public partial class PowerSetter : ServiceBase {
		readonly string AppDataPath = @"C:\Users\Thomas Fox\AppData\Roaming";
		readonly string AppName = "Powerplan Changer";
		readonly string SettingsFileName = "settings.xml";
		readonly string logFileName = "log.txt";
		readonly int interval = 10; //seconds

		Timer timer;

		List<PowerPlan> powerPlans = new List<PowerPlan>();
		List<SystemProcess> systemProcesses = new List<SystemProcess>();
		PowerPlan currentPlan = null;

		public PowerSetter() {
			InitializeComponent();
			timer = new Timer();
		}

		private void PrintToLog(string text) {
			StreamWriter sw = File.AppendText($"{AppDataPath}\\{AppName}\\{logFileName}");
			sw.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}] {text}");
			sw.Close();
		}

		//We create the directory and a basic xml file with the correct structure
		private bool CreateSettingsFolder(string path) {
			try {
				Directory.CreateDirectory(path);

				XmlDocument doc = new XmlDocument();
				XmlElement root = doc.CreateElement("Settings");

				XmlElement powerPlans = doc.CreateElement("PowerPlans");
				XmlElement powerPlan = doc.CreateElement("PowerPlan");
				XmlElement ppName = doc.CreateElement("Name");
				ppName.InnerXml = ".";
				XmlElement ppID = doc.CreateElement("ID");
				ppID.InnerXml = ".";
				powerPlan.AppendChild(ppName);
				powerPlan.AppendChild(ppID);
				powerPlans.AppendChild(powerPlan);

				XmlElement processes = doc.CreateElement("Processes");
				XmlElement process = doc.CreateElement("Process");
				process.SetAttribute("plan", "");
				process.InnerXml = ".";
				processes.AppendChild(process);

				root.AppendChild(powerPlans);
				root.AppendChild(processes);
				
				doc.AppendChild(root);

				doc.Save($"{path}\\{SettingsFileName}");
			} catch (Exception ex) {
				PrintToLog(ex.Message);
				return false;
			}
			return true;
		}

		//We load up the settings file and add the power plans as well as the processes to look out for
		private bool LoadConfig(string path) {
			try {
				XmlDocument doc = new XmlDocument();
				doc.Load(path);
				XmlElement root = doc.DocumentElement;
				foreach (XmlElement rootTags in root.ChildNodes) {
					if (rootTags.Name == "PowerPlans") {
						powerPlans.Clear();
						foreach (XmlElement powerPlanTag in rootTags.ChildNodes) {
							string name = "";
							string id = "";
							bool valid = true;
							foreach (XmlElement tag in powerPlanTag.ChildNodes) {
								//If the node contains useless value, we discard it
								if (tag.InnerXml.Length == 0 || tag.InnerXml == ".") {
									valid = false;
									break;
                                }
								if (tag.Name == "Name") name = tag.InnerXml;
								else if (tag.Name == "ID") id = tag.InnerXml;
							}
							if (!valid) continue;
							powerPlans.Add(new PowerPlan(name, id));
						}
					} else if (rootTags.Name == "Processes") {
						systemProcesses.Clear();
						foreach (XmlElement processTag in rootTags.ChildNodes) {
							//If the node contains useless value, we discard it
							if (processTag.InnerXml.Length == 0 || processTag.InnerXml == ".") continue;
							systemProcesses.Add(new SystemProcess(processTag.InnerXml.ToLower(), GetPowerPlan(processTag.GetAttribute("plan"))));	
                        }
					}
				}
			} catch (Exception ex) {
				PrintToLog(ex.Message);
				return false;
			}
			return true;
		}

		//We check if every directory and appropriate file exists, and attempt to start the service
		private bool AttemptStartup() {
			string AppSettingsFolder = $"{AppDataPath}\\{AppName}";
			if (Directory.Exists(AppSettingsFolder)) {
				string SettingsFile = $"{AppSettingsFolder}\\{SettingsFileName}";
				if (File.Exists(SettingsFile)) return LoadConfig(SettingsFile);
				else return false;
			} else {
				//If main directory doesn't exist we create it and try again
				if (!CreateSettingsFolder(AppSettingsFolder)) return false;
				PrintToLog("Successfully created directory and file");
				return AttemptStartup();
			}
		}

		private PowerPlan GetPowerPlan(string name) {
			foreach (PowerPlan p in powerPlans) if (p.Name == name) return p;
			return powerPlans[0];
		}

		//If the current power plan is not the same as the one we want to set it to, we start a new process and change the power plan
		private void SetPowerPlan(PowerPlan plan) {
			if (currentPlan == plan) return;
			// /c needed to run command (solution 4 https://www.codeproject.com/Questions/561173/Runningpluscommandpluspromptpluscommandsplususingp)
			Process.Start("cmd.exe", $"/c powercfg.exe /setactive {plan.Id}");
			if (currentPlan != null) PrintToLog($"{currentPlan.Name} -> {plan.Name}");
			else PrintToLog($"... -> {plan.Name}");
			currentPlan = plan;
		}

		//We check every process in the list, if we find one we set the power plan accordingly
		private void CheckProcesses() {
			bool found = false;
			foreach (Process runningProcess in Process.GetProcesses()) {
				if (found) break;
				foreach (SystemProcess process in systemProcesses) {
					string name = runningProcess.ProcessName.ToLower();
					if (name == process.Name) {
						SetPowerPlan(process.Plan);
						found = true;
						break;
					}
				}
			}
			//If we found none, we set it to the default power plan
			if (!found) SetPowerPlan(powerPlans[0]);
		}

		private void timerCheckProcesses_Tick(object sender, EventArgs e) {
			CheckProcesses();
		}

		protected override void OnStart(string[] args) {
			//If startup attempts fail, we exit
			if (!AttemptStartup()) return;
			//If we detect neither power plans or processes to check, we exit
			if (powerPlans.Count == 0 || systemProcesses.Count == 0) return;
			PrintToLog("Powerplan Changer started");
			//We set the power plan to the default one, which is always the first in the list
			SetPowerPlan(powerPlans[0]);
			timer.Interval = interval * 1000;
			timer.Elapsed += new ElapsedEventHandler(timerCheckProcesses_Tick);
			timer.Enabled = true;
		}
		protected override void OnStop() {
			if (timer.Enabled) timer.Enabled = false;
			PrintToLog("Powerplan Changer stopped");
		}
	}
}
