using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Common;

namespace gp
{
    public class Program
    {
        #region Symbols enum

        #endregion

        private const int LeafCount = (Symbols.WorkingVariable - Symbols.InputArgument) + 1;

        private readonly ConstantsSet _constantsSet;
        private readonly int _varnumber;
        private List<int> _code;

        private Program(int varnumber, List<int> code, ConstantsSet constantsSet, int workingVariablesCount)
        {
            _varnumber = varnumber;
            _code = code;
            _constantsSet = constantsSet;
            WorkingVariablesCount = workingVariablesCount;
            //For testing:
            Traverse(0);
        }

        public Program(Random rd, int depth, int varnumber) : this(rd, depth, varnumber, null, 0)
        {
        }

        public Program(Program parent) : this(parent._varnumber, parent._code, 
            parent._constantsSet, parent.WorkingVariablesCount)
        {
        }

        public Program(int varnumber, IEnumerable<int> code, ConstantsSet constantsSet, int workingVariableCount)
        {
            _varnumber = varnumber;
            _code = new List<int>(code);
            _constantsSet = constantsSet;
            WorkingVariablesCount = workingVariableCount;
        }

        private Program(Random rd, int depth, int varnumber, ConstantsSet constantsSet, int workingVariablesCount)
        {
            _varnumber = varnumber;
            _constantsSet = _constantsSet == null ? new ConstantsSet() : constantsSet;
            WorkingVariablesCount = workingVariablesCount;
            _code = new List<int>(Grow(rd, depth));
        }

        public List<int> Code
        {
            get { return _code; }
        }

        public int Varnumber
        {
            get { return _varnumber; }
        }

        public ConstantsSet Constants
        {
            get { return _constantsSet; }
        }

        public int WorkingVariablesCount { get; set; }

        public static void PurgeUnusedConstants(int varnumber, ref List<int> code, ref ConstantsSet constantsSet)
        {
            var newConstants = new ConstantsSet(constantsSet.NormalDistribution);
            var newCode = new List<int>();
            foreach (int instruction in code)
            {
                Symbols symbol;
                int qualifier;
                DecodeSymbol(varnumber, instruction, out symbol, out qualifier);
                switch (symbol)
                {
                    case Symbols.IntegerLiteral:
                        newCode.Add(Codec.EncodeSymbol(varnumber, symbol,
                                                 newConstants.Integers.Include(constantsSet.Integers[qualifier])));
                        break;
                    case Symbols.DoubleLiteral:
                        newCode.Add(Codec.EncodeSymbol(varnumber, symbol,
                                                 newConstants.Doubles.Include(constantsSet.Doubles[qualifier])));
                        break;
                    default:
                        newCode.Add(instruction);
                        break;
                }
            }
            code = newCode;
            constantsSet = newConstants;
        }

        private IEnumerable<int> Grow(Random rd, int depth)
        {
            //First pick a random symbol.
            Symbols symbol = depth <= 0 ? PickRandomLeaf(rd) : PickRandomSymbol(rd);
            if (NeedsQualifier(symbol))
            {
                //Some symbols need an extra token
                int qualifier = 0;
                switch (symbol)
                {
                    //Select an input argument
                    case Symbols.InputArgument:
                        qualifier = rd.Next(Varnumber);
                        break;
                    case Symbols.IntegerLiteral:
                        qualifier = Constants.PickIntegerConstant(rd);
                        break;
                    case Symbols.DoubleLiteral:
                        qualifier = Constants.PickDoubleConstant(rd);
                        break;
                    case Symbols.WorkingVariable:
                    case Symbols.AssignWorkingVariable:
                        {
                            qualifier = rd.Next(WorkingVariablesCount + 1);
                            if (qualifier >= WorkingVariablesCount)
                            {
                                ++WorkingVariablesCount;
                            }
                        }
                        break;
                }
                yield return Codec.EncodeSymbol(Varnumber, symbol, qualifier);
            }
            else
            {
                yield return Codec.EncodeSymbol(Varnumber, symbol);
            }
            //Find the number of necessary children and select each one.
            int arity = GetSyntacticArity(symbol);
            for (int i = 0; i < arity; ++i)
            {
                foreach (int c in Grow(rd, depth - 1))
                {
                    yield return c;
                }
            }
        }

        int FindRelativelyShortSubTree(Random rd, int len, out int endOfNode, int iterations = 3)
        {
            int node = rd.Next(len);
            endOfNode = Traverse(node);
            int nodeLen = (endOfNode - node) + 1;
            for (int i = 1; i < iterations; ++i)
            {
                int node2 = rd.Next(len);
                int endOfNode2 = Traverse(node2);

                int nodeLen2 = (endOfNode2 - node2) + 1;

                if (nodeLen2 < nodeLen)
                {
                    node = node2;
                    endOfNode = endOfNode2;
                    nodeLen = nodeLen2;
                }
            }

            return node;
        }

        public Program Crossover(Random rd, Program parent)
        {
            int len = Traverse(0);
            int parentLen = parent.Traverse(0);
            int nodeToRemove = FindRelativelyShortSubTree(rd, len, out int endOfNodeToRemove);
            int nodeToInsert = parent.FindRelativelyShortSubTree(rd, parentLen, out int endOfNodeToInsert);

            var childCode = new List<int>(len + nodeToRemove - endOfNodeToRemove + endOfNodeToInsert - nodeToInsert);
            IList<int> integerRhs2MergedMapping;
            IList<int> doubleRhs2MergedMapping;
            ConstantsSet childConstants = Constants.Merge(parent.Constants, out integerRhs2MergedMapping,
                                                          out doubleRhs2MergedMapping);

            childCode.AddRange(Code.GetRange(0, nodeToRemove));
            int varnumber = Math.Max(_varnumber, parent._varnumber);
            int workingVarialbesCount = Math.Max(WorkingVariablesCount, parent.WorkingVariablesCount);
            for (int i = nodeToInsert; i < endOfNodeToInsert; ++i)
            {
                childCode.Add(RecodeSymbol(varnumber, parent.Code[i], integerRhs2MergedMapping, doubleRhs2MergedMapping));
            }
            childCode.AddRange(Code.GetRange(endOfNodeToRemove, len - endOfNodeToRemove));

            if (rd.NextDouble() <= 0.5)
            {
                PurgeUnusedConstants(varnumber, ref childCode, ref childConstants);
            }
            if (rd.NextDouble() <= 0.5)
            {
                CompactWorkingVariables(varnumber, childCode, ref workingVarialbesCount);
            }
            return new Program(varnumber, childCode, childConstants, workingVarialbesCount);
        }

        private static void CompactWorkingVariables(int varnumber, IList<int> code, ref int workingVarialbesCount)
        {
            var variableMapping = new Dictionary<int, int>();
            for (int i = 0; i < code.Count; ++i)
            {
                Symbols symbol;
                int qualifier;
                DecodeSymbol(varnumber, code[i], out symbol, out qualifier);
                if (symbol == Symbols.WorkingVariable)
                {
                    int newQualifier;
                    if (variableMapping.TryGetValue(qualifier, out newQualifier))
                    {
                        qualifier = newQualifier;
                    }
                    else
                    {
                        variableMapping[qualifier] = variableMapping.Count;
                    }
                    code[i] = Codec.EncodeSymbol(varnumber, symbol, qualifier);
                }
            }
            workingVarialbesCount = variableMapping.Count;
        }

        public void Skip(ref int pc, out int maximumDepth)
        {
            Skip(1, ref pc, out maximumDepth);
        }

        private void Skip(int steps, ref int pc, out int maximumDepth)
        {
            maximumDepth = 0;
            for (int i = 0; i < steps; ++i)
            {
                Symbols symbol;
                int qualifier;
                DecodeSymbol(Varnumber, Code[pc++], out symbol, out qualifier);

                int depth;
                Skip(GetSyntacticArity(symbol), ref pc, out depth);
                if (depth + 1 > maximumDepth)
                {
                    maximumDepth = depth + 1;
                }
            }
        }

        public int Traverse(int pc)
        {
            bool was0 = pc == 0;
            int maximumDepth;
            Skip(ref pc, out maximumDepth);
            if (was0)
            {
                if (pc != Code.Count)
                {
                    int newPc = 0;
                    new CallTree(this, ref newPc);
                    Print();
                    if (Code.Count > pc)
                    {
                        var missingCallTree = new CallTree(this, ref newPc);
                        var missingProgram = new Program(Varnumber, new List<int>(missingCallTree.Encode()),
                                                         _constantsSet, WorkingVariablesCount);
                        missingProgram.Print();
                    }
                    throw new Exception("Traverse(0) renders wrong length");
                }
            }
            return pc;
        }

        public Program Mutate(Random rd, double pmut)
        {
            bool mutated = false;
            Program mutatedProgram = this;
            int len = Traverse(0);
            while (!mutated)
            {
                if (rd.NextDouble() < pmut)
                {
                    //Subtree replacement is implemented through crossover with a new randomly generated program
                    return Crossover(rd,
                        new Program(rd, Gp.Depth, _varnumber, new ConstantsSet(_constantsSet),
                            WorkingVariablesCount));
                }

                /*if (rd.NextDouble() < pmut)
                {
                    //Simplify this program as efficient as possible
                    return Simplify();
                }*/

                mutatedProgram = new Program(this);
                //len = mutatedProgram.Traverse(0);

                for (int i = 0; i < len; ++i)
                {
                    if (rd.NextDouble() < pmut)
                    {
                        Symbols symbol;
                        int qualifier;
                        DecodeSymbol(Varnumber, mutatedProgram.Code[i], out symbol, out qualifier);
                        if (symbol == Symbols.IntegerLiteral)
                        {
                            if (rd.NextDouble() < pmut)
                            {
                                //It is a constant. We choose to adjust the constant value slightly.
                                //Code remains unchanged.
                                mutatedProgram.Constants.MutateIntegerConstant(rd, qualifier);
                                mutated = true;
                                continue;
                            }
                        }
                        else if (symbol == Symbols.DoubleLiteral)
                        {
                            if (rd.NextDouble() < pmut)
                            {
                                //It is a constant. We choose to adjust the constant value slightly.
                                //Code remains unchanged.
                                mutatedProgram.Constants.MutateDoubleConstant(rd, qualifier);
                                mutated = true;
                                continue;
                            }
                        }

                        int arity = GetSyntacticArity(symbol);


                        if (arity > 0)
                        {
                            if (rd.NextDouble() < pmut)
                            {
                                //Replace this node with one of it's children (or a new constant if possible)
                                mutatedProgram.ReplaceNodeWithChild(rd, i);
                                //Determine the new length
                                len = mutatedProgram.Traverse(0);
                                mutated = true;
                                continue;
                            }

                            if (rd.NextDouble() < pmut)
                            {
                                mutatedProgram.ReshuffleChildren(rd, i);
                                mutated = true;
                                continue;
                            }
                        }

                        /*if (rd.NextDouble() < pmut)
                        {
                            //Simplifies the expression at the given node.
                            Program simplifiedProgram = mutatedProgram.SimplifyNode(i);
                            if (simplifiedProgram != null)
                            {
                                return simplifiedProgram;
                            }
                        }*/

                        if (rd.NextDouble() < pmut)
                        {
                            len = ReplaceSymbol(rd, symbol, mutatedProgram, arity, i, len);

                            mutated = true;
                        }
                    }
                }
            }

            //For testing:
            mutatedProgram.Traverse(0);
            return mutatedProgram;
        }

        private int ReplaceSymbol(Random rd, Symbols currentSymbol, Program mutatedProgram, int arity, int i, int len)
        {
            Symbols newSymbol = currentSymbol;
            while (newSymbol == currentSymbol)
            {
                newSymbol = PickRandomSymbol(rd);
            }

            int? newQualifier = null;

            if (NeedsQualifier(newSymbol))
            {
                switch (newSymbol)
                {
                    //Select an input argument
                    case Symbols.InputArgument:
                        newQualifier = rd.Next(mutatedProgram.Varnumber);
                        break;
                    case Symbols.IntegerLiteral:
                        newQualifier = mutatedProgram.Constants.PickIntegerConstant(rd);
                        break;
                    case Symbols.DoubleLiteral:
                        newQualifier = mutatedProgram.Constants.PickDoubleConstant(rd);
                        break;
                    case Symbols.WorkingVariable:
                    {
                        newQualifier = rd.Next(mutatedProgram.WorkingVariablesCount + 1);
                        if (newQualifier >= mutatedProgram.WorkingVariablesCount)
                        {
                            ++mutatedProgram.WorkingVariablesCount;
                        }

                        break;
                    }
                    case Symbols.AssignWorkingVariable:
                    {
                        newQualifier = rd.Next(mutatedProgram.WorkingVariablesCount + 1);
                        if (newQualifier >= mutatedProgram.WorkingVariablesCount)
                        {
                            ++mutatedProgram.WorkingVariablesCount;
                        }

                        break;
                    }
                }
            }

            int newArity = GetSyntacticArity(newSymbol);

            if (newArity != arity)
            {
                // Our new symbol has a different arity so we need to remove or squeeze in some new children
                //First we analyse this node's children.
                int childEnd = mutatedProgram.TraverseChildren(i, out IList<IList<int>> children, out var maximumDepth);
                //Then we remove them from the code.
                mutatedProgram.Code.RemoveRange(i + 1, childEnd - (i + 1));
                while (children.Count < newArity)
                {
                    //Our new symbol has a higher arity: grow some random nodes but limit them to the depth
                    //our node already has. Insert them at random locations between the existing children.
                    children.Insert(rd.Next(children.Count + 1),
                        new List<int>(mutatedProgram.Grow(rd, maximumDepth)));
                }

                while (children.Count > newArity)
                {
                    //Our new symbol has a lower arity: As long as we still have too many children remove one at random.
                    children.RemoveAt(rd.Next(children.Count));
                }

                //Then we put the new and the old children back in the code.
                int k = i;
                foreach (var child in children)
                {
                    foreach (int token in child)
                    {
                        mutatedProgram.Code.Insert(++k, token);
                    }
                }

                //And then we trim it up.
                mutatedProgram.Code.TrimExcess();
            }

            mutatedProgram.Code[i] = Codec.EncodeSymbol(Varnumber, newSymbol, newQualifier);

            if (newArity != arity)
            {
                //Determine the new length
                len = mutatedProgram.Traverse(0);
            }

            return len;
        }

        private void ReshuffleChildren(Random rd, int ix)
        {
            //reorganise the children in a random order.
            IList<IList<int>> children;
            //First we analyse this node's children.
            TraverseChildren(ix, out children);
            while (children.Count > 0)
            {
                //We just pick all the children at random and insert their code back.
                int selectedChildIndex = rd.Next(children.Count);

                foreach (int token in children[selectedChildIndex])
                {
                    _code[++ix] = token;
                }
                children.RemoveAt(selectedChildIndex);
            }
        }

        private void TraverseChildren(int ix, out IList<IList<int>> children)
        {
            int maximumDepth;
            TraverseChildren(ix, out children, out maximumDepth);
        }

        private int TraverseChildren(int ix, out IList<KeyValuePair<int, int>> childrenBoundaries)
        {
            int maximumDepth;
            return TraverseChildren(ix, out childrenBoundaries, out maximumDepth);
        }


        private int TraverseChildren(int ix, out IList<KeyValuePair<int, int>> childrenBoundaries, out int maximumDepth)
        {
            Symbols symbol;
            int qualifier;
            DecodeSymbol(Varnumber, _code[ix], out symbol, out qualifier);
            int arity = GetSyntacticArity(symbol);

            int childStart = ix + 1;
            int childEnd = childStart;
            childrenBoundaries = new List<KeyValuePair<int, int>>();
            maximumDepth = 0;
            for (int i = 0; i < arity; ++i)
            {
                Symbols childSymbol;
                int childQualifier;
                DecodeSymbol(Varnumber, _code[childStart], out childSymbol, out childQualifier);

                int depth;
                childEnd = Traverse(childStart, out depth);
                if (depth + 1 > maximumDepth)
                {
                    maximumDepth = depth + 1;
                }
                childrenBoundaries.Add(new KeyValuePair<int, int>(childStart, childEnd));
                childStart = childEnd;
            }
            return childEnd;
        }

        private int TraverseChildren(int ix, out IList<IList<int>> children, out int maximumDepth)
        {
            children = new List<IList<int>>();

            IList<KeyValuePair<int, int>> childrenBoundaries;
            int childEnd = TraverseChildren(ix, out childrenBoundaries, out maximumDepth);
            foreach (var kvp in childrenBoundaries)
            {
                children.Add(_code.GetRange(kvp.Key, kvp.Value - kvp.Key));
            }
            return childEnd;
        }

        private int Traverse(int pc, out int maximumDepth)
        {
            Skip(ref pc, out maximumDepth);
            return pc;
        }

        private void ReplaceNodeWithChild(Random rd, int ix)
        {
            IList<KeyValuePair<int, int>> childrenBoundaries;
            int childEnd = TraverseChildren(ix, out childrenBoundaries);

            if (childrenBoundaries.Count > 0)
            {
                //If we did not replace a constant expression with a literal then pick one of the children at random.
                KeyValuePair<int, int> selectedChild = childrenBoundaries[rd.Next(childrenBoundaries.Count)];
                var code = new List<int>(_code.Count + ix + selectedChild.Value - selectedChild.Key - childEnd);
                code.AddRange(_code.GetRange(0, ix));
                code.AddRange(_code.GetRange(selectedChild.Key, selectedChild.Value - selectedChild.Key));
                code.AddRange(_code.GetRange(childEnd, _code.Count - childEnd));
                _code = code;
            }
        }

        public void Print()
        {
            Console.WriteLine(ToString());
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            Print(0, stringBuilder, false);
            return stringBuilder.ToString();
        }

        public string ToCSharp()
        {
            var stringBuilder = new StringBuilder();
            Print(0, stringBuilder, true);
            return stringBuilder.ToString();
        }

        private int Print(int pc, StringBuilder stringBuilder, bool appendMToDecimals)
        {
            Symbols symbol;
            int qualifier;
            DecodeSymbol(Varnumber, Code[pc++], out symbol, out qualifier);
            int arity = GetSyntacticArity(symbol);
            if (arity > 0) //Is not a leaf
            {
                stringBuilder.Append("(");

                if (arity == 2)
                {
                    //LHS of a binary operator

                    switch (symbol)
                    {
                        case Symbols.If:
                            stringBuilder.Append("IF ");
                            break;
                        case Symbols.While:
                            stringBuilder.Append("WHILE ");
                            break;
                    }

                    pc = Print(pc, stringBuilder, appendMToDecimals);
                }
            }

            switch (symbol)
            {
                case Symbols.InputArgument:
                    stringBuilder.Append(string.Format("X{0}", qualifier));
                    break;
                case Symbols.IntegerLiteral:
                    stringBuilder.Append(string.Format("{0}", Constants.Integers[qualifier]));
                    break;
                case Symbols.DoubleLiteral:
                    stringBuilder.Append(Constants.Doubles[qualifier].ToString(CultureInfo.GetCultureInfo("en-GB").NumberFormat));
                    if (appendMToDecimals)
                    {
                        stringBuilder.Append("m");
                    }

                    break;
                case Symbols.WorkingVariable:
                    stringBuilder.Append(string.Format("Y{0}", qualifier));
                    return pc;
                case Symbols.AssignWorkingVariable:
                    stringBuilder.Append(string.Format("Y{0}:=", qualifier));
                    break;
                case Symbols.Not:
                    stringBuilder.Append("NOT ");
                    break;
                case Symbols.If:
                    stringBuilder.Append(" THEN ");
                    break;
                case Symbols.While:
                    stringBuilder.Append(" DO ");
                    break;
                case Symbols.Mov:
                    stringBuilder.Append("X[");
                    pc = Print(pc, stringBuilder, appendMToDecimals);
                    stringBuilder.Append("]:=");
                    return Print(pc, stringBuilder, appendMToDecimals);
                case Symbols.Ifelse:
                    stringBuilder.Append("IF ");
                    pc = Print(pc, stringBuilder, appendMToDecimals);
                    stringBuilder.Append(" THEN ");
                    pc = Print(pc, stringBuilder, appendMToDecimals);
                    stringBuilder.Append(" ELSE ");
                    break;
                case Symbols.Add:
                    stringBuilder.Append(" + ");
                    break;
                case Symbols.Sub:
                    stringBuilder.Append(" - ");
                    break;
                case Symbols.Mul:
                    stringBuilder.Append(" * ");
                    break;
                case Symbols.Div:
                    stringBuilder.Append(" / ");
                    break;
                case Symbols.Lt:
                    stringBuilder.Append(" < ");
                    break;
                case Symbols.Lteq:
                    stringBuilder.Append(" <= ");
                    break;
                case Symbols.Gt:
                    stringBuilder.Append(" > ");
                    break;
                case Symbols.Gteq:
                    stringBuilder.Append(" >= ");
                    break;
                case Symbols.Eq:
                    stringBuilder.Append(" = ");
                    break;
                case Symbols.Neq:
                    stringBuilder.Append(" <> ");
                    break;
                case Symbols.And:
                    stringBuilder.Append(" AND ");
                    break;
                case Symbols.Or:
                    stringBuilder.Append(" OR ");
                    break;
                case Symbols.Xor:
                    stringBuilder.Append(" XOR ");
                    break;
                case Symbols.Chain:
                    stringBuilder.Append("=>");
                    break;
                case Symbols.Noop:
                    stringBuilder.Append("Noop");
                    break;
            }

            if (arity > 0)
            {
                pc = Print(pc, stringBuilder, appendMToDecimals);
                //Was not a leaf
                stringBuilder.Append(")");
            }
            return pc;
        }

        private static int RecodeSymbol(int varnumber, int primitive, IList<int> integerRhs2MergedMapping,
                                        IList<int> doubleRhs2MergedMapping)
        {
            Symbols symbol;
            int qualifier;
            DecodeSymbol(varnumber, primitive, out symbol, out qualifier);
            switch (symbol)
            {
                case Symbols.IntegerLiteral:
                    return Codec.EncodeSymbol(varnumber, symbol, integerRhs2MergedMapping[qualifier]);
                case Symbols.DoubleLiteral:
                    return Codec.EncodeSymbol(varnumber, symbol, doubleRhs2MergedMapping[qualifier]);
                default:
                    return primitive;
            }
        }

        //Simplify the language
        private static HashSet<Symbols> bannedSymbols = new HashSet<Symbols>
        {
            Symbols.Not,
            Symbols.Lt,
            Symbols.Lteq,
            Symbols.Gt,
            Symbols.Gteq,
            Symbols.Eq,
            Symbols.Neq,
            Symbols.And,
            Symbols.Or,
            Symbols.Xor,
            Symbols.Chain,
            Symbols.If,
            Symbols.While,
            Symbols.Mov,
            Symbols.Ifelse,
            Symbols.WorkingVariable,
            Symbols.AssignWorkingVariable,
        }; 

        private static Symbols PickRandomSymbol(Random rand)
        {
            Symbols symbol;
            do
            {
                symbol = PickRandomSymbolImpl(rand);
            } while (bannedSymbols.Contains(symbol));
            return symbol;
        }

        private static Symbols PickRandomSymbolImpl(Random rand)
        {
            return (Symbols)(rand.Next((int)Symbols.AssignWorkingVariable) + 1);
        }

        private static Symbols PickRandomLeaf(Random rand)
        {
            Symbols symbol;
            do
            {
                symbol = PickRandomLeafImpl(rand);
            } while (bannedSymbols.Contains(symbol));
            return symbol;
        }

        private static Symbols PickRandomLeafImpl(Random rand)
        {
            //Return a random synbol with arity 0.
            return (Symbols)rand.Next(LeafCount) + (int)Symbols.InputArgument;
        }

        private static bool NeedsQualifier(Symbols op)
        {
            //True if the symbol means nothing on it's own and needs a literal argument that is part of the symbol itself
            return op >= Symbols.InputArgument;
        }

        public static int GetSyntacticArity(Symbols op)
        {
            switch (op)
            {
                case Symbols.InputArgument:
                case Symbols.IntegerLiteral:
                case Symbols.DoubleLiteral:
                case Symbols.WorkingVariable:
                case Symbols.Noop:
                    return 0;
                case Symbols.AssignWorkingVariable:
                case Symbols.Not:
                    return 1;
                case Symbols.Ifelse:
                    return 3;
                default:
                    return 2;
            }
        }

        public static int GetDependencyArity(Symbols op)
        {
            switch (op)
            {
                case Symbols.IntegerLiteral:
                case Symbols.InputArgument:
                case Symbols.DoubleLiteral:
                case Symbols.WorkingVariable:
                case Symbols.While:
                case Symbols.Noop:
                    return 0;
                case Symbols.AssignWorkingVariable:
                case Symbols.Not:
                case Symbols.If:
                case Symbols.And:
                case Symbols.Or:
                case Symbols.Ifelse:
                    return 1;
                default:
                    return 2;
            }
        }

        public static void DecodeSymbol(int varnumber, int primitive, out Symbols symbol, out int qualifier)
        {
            if (primitive < (int)Symbols.InputArgument)
            {
                symbol = (Symbols) primitive;
                qualifier = 0;
                return;
            }
            primitive -= (int)Symbols.InputArgument;
            if (primitive < varnumber)
            {
                symbol = Symbols.InputArgument;
                qualifier = primitive;
                return;
            }
            primitive -= varnumber;

            switch (primitive%4)
            {
                case 0:
                    symbol = Symbols.WorkingVariable;
                    break;
                case 1:
                    symbol = Symbols.AssignWorkingVariable;
                    break;
                case 2:
                    symbol = Symbols.DoubleLiteral;
                    break;
                default:
                    symbol = Symbols.IntegerLiteral;
                    break;
            }
            qualifier = primitive/4;
        }



        private Program SimplifyNode(int ix)
        {
            var callTree = new CallTree(_varnumber, _code, ref ix);
            var constantsSet = new ConstantsSet(_constantsSet);
            CallTree simplifiedCallTree = callTree.Simplify(constantsSet);
            if (simplifiedCallTree != null)
            {
                return new Program(_varnumber, new List<int>(simplifiedCallTree.Encode()), constantsSet,
                                   WorkingVariablesCount);
            }
            return null;
        }

        public Program Simplify()
        {
            return Simplify(null);
        }

        public Program Simplify(FitnessEvaluation fitnessEvaluation)
        {
            int pc = 0;
            var callTree = new CallTree(_varnumber, _code, ref pc);
            var constantsSet = new ConstantsSet(_constantsSet);
            CallTree simplifiedCallTree;
            if (fitnessEvaluation != null)
            {
                Print();
            }
            do
            {
                callTree.Reset();
                simplifiedCallTree = callTree.Simplify(constantsSet, fitnessEvaluation != null);
                if (fitnessEvaluation != null && simplifiedCallTree != null)
                {
                    Console.WriteLine("After removing redundant code:");

                    var simplifiedprogram = new Program(_varnumber, new List<int>(simplifiedCallTree.Encode()),
                                                        constantsSet,
                                                        WorkingVariablesCount);
                    simplifiedprogram.Print();
                    if (AnalyzeTestCasedifferences(fitnessEvaluation, simplifiedprogram))
                    {
                        break;
                    }
                }
                CallTree tmpCallTree =
                    (simplifiedCallTree ?? callTree).PurgeUnassignedVariables(constantsSet,
                                                                              new List<int>());

                if (tmpCallTree != null)
                {
                    simplifiedCallTree = tmpCallTree;
                    if (fitnessEvaluation != null)
                    {
                        Console.WriteLine("After removing unassigned variables:");
                        var simplifiedprogram = new Program(_varnumber, new List<int>(simplifiedCallTree.Encode()),
                                                            constantsSet,
                                                            WorkingVariablesCount);
                        simplifiedprogram.Print();
                        if (AnalyzeTestCasedifferences(fitnessEvaluation, simplifiedprogram))
                        {
                            break;
                        }
                    }
                }

                tmpCallTree =
                    (simplifiedCallTree ?? callTree).PurgeUnusedAssignmentsToVariables(constantsSet, new List<int>());

                if (tmpCallTree != null)
                {
                    simplifiedCallTree = tmpCallTree;
                    if (fitnessEvaluation != null)
                    {
                        Console.WriteLine("After removing assignments to variables not used again:");
                        var simplifiedprogram = new Program(_varnumber, new List<int>(simplifiedCallTree.Encode()),
                                                            constantsSet,
                                                            WorkingVariablesCount);
                        simplifiedprogram.Print();
                        if (AnalyzeTestCasedifferences(fitnessEvaluation, simplifiedprogram))
                        {
                            break;
                        }
                    }
                }

                tmpCallTree = (simplifiedCallTree ?? callTree).PurgeRedundantUsagesOfAssignedVariables(constantsSet, new Dictionary<int, CallTree>());

                if (tmpCallTree != null)
                {
                    simplifiedCallTree = tmpCallTree;
                    if (fitnessEvaluation != null)
                    {
                        Console.WriteLine("After removing redundant usages of assigned variables:");
                        var simplifiedprogram = new Program(_varnumber, new List<int>(simplifiedCallTree.Encode()),
                                                            constantsSet,
                                                            WorkingVariablesCount);
                        simplifiedprogram.Print();
                        if (AnalyzeTestCasedifferences(fitnessEvaluation, simplifiedprogram))
                        {
                            break;
                        }
                    }
                }

                if (simplifiedCallTree != null && simplifiedCallTree.Equals(callTree))
                {
                    Console.WriteLine("Simplified call tree is equal to original call tree, giving up");
                    new Program(_varnumber, new List<int>(callTree.Encode()), constantsSet, WorkingVariablesCount).Print
                        ();
                    //break;
                }
                callTree = simplifiedCallTree ?? callTree;
            } while (simplifiedCallTree != null);
            return new Program(_varnumber, new List<int>(callTree.Encode()), constantsSet, WorkingVariablesCount);
        }

        public bool AnalyzeTestCasedifferences(FitnessEvaluation fitnessEvaluation, Program simplifiedProgram)
        {
            //We need to evaluate unknown test-cases too.
            fitnessEvaluation.Evaluate(false, true);

            var simplifiedFitnessEvaluation = new FitnessEvaluation(simplifiedProgram, fitnessEvaluation.Problem);
            simplifiedFitnessEvaluation.Evaluate(false, true, fitnessEvaluation.Results);
            for (int i = 0; i < fitnessEvaluation.Results.Length; ++i)
            {
                if (fitnessEvaluation.Results[i] != simplifiedFitnessEvaluation.Results[i])
                {
                    return true;
                }
            }
            return false;
        }
    }
}