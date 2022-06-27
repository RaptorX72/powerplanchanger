namespace PowerplanChanger {
    class SystemProcess {
        string name;
        PowerPlan plan;

        public string Name { get => name; }
        internal PowerPlan Plan { get => plan; }

        public SystemProcess(string name, PowerPlan plan) {
            this.name = name;
            this.plan = plan;
        }
    }
}
