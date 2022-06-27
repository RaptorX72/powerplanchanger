namespace PowerplanChanger {
	class PowerPlan {
		private string name;
		private string id;

		public string Name { get => name; set => name = value; }
		public string Id { get => id; set => id = value; }

		public PowerPlan(string name, string id) {
			this.name = name;
			this.id = id;
		}
	}
}
