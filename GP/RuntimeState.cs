namespace gp
{
    public class RuntimeState
    {
        private readonly decimal[] _inputs;
        private readonly VariableSet _variables;

        public RuntimeState(int varnumber)
        {
            _inputs = new decimal[varnumber];
            _variables = new VariableSet();
        }

        public RuntimeState(RuntimeState runtimeState)
        {
            _inputs = new decimal[runtimeState._inputs.Length];
            runtimeState._inputs.CopyTo(_inputs, 0);
            _variables = new VariableSet(runtimeState.Variables);
        }

        public decimal[] Inputs => _inputs;

        public VariableSet Variables => _variables;

        public int Pc { get; set; }
    }
}