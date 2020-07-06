namespace gp
{
    public class RuntimeState
    {
        private readonly double[] _inputs;
        private readonly VariableSet _variables;

        public RuntimeState(int varnumber)
        {
            _inputs = new double[varnumber];
            _variables = new VariableSet();
        }

        public RuntimeState(RuntimeState runtimeState)
        {
            _inputs = new double[runtimeState._inputs.Length];
            runtimeState._inputs.CopyTo(_inputs, 0);
            _variables = new VariableSet(runtimeState.Variables);
        }

        public double[] Inputs => _inputs;

        public VariableSet Variables => _variables;

        public int Pc { get; set; }
    }
}