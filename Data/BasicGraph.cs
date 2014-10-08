using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonLD.Core;
using Node = JsonLD.Core.RDFDataset.Node;
using System.Globalization;

namespace NuGet.Data
{
    public class BasicGraph
    {
        private readonly HashSet<Triple> _triples;

        public BasicGraph()
            : this(new HashSet<Triple>())
        {

        }

        public BasicGraph(IEnumerable<Triple> triples)
        {
            _triples = new HashSet<Triple>(triples);
        }

        public void Assert(Triple triple)
        {
            Triples.Add(triple);
        }

        public void Assert(RDFDataset.Quad quad)
        {
            Triples.Add(new Triple(quad.GetSubject(), quad.GetPredicate(), quad.GetObject()));
        }

        public void Assert(RDFDataset.Node subNode, RDFDataset.Node predNode, RDFDataset.Node objNode)
        {
            Triples.Add(new Triple(subNode, predNode, objNode));
        }

        public void Merge(BasicGraph graph)
        {
            foreach (var triple in graph.Triples)
            {
                Triples.Add(triple);
            }
        }

        public BasicGraph RecursiveDescribe(Uri entity)
        {
            HashSet<Triple> triples = new HashSet<Triple>();
            RecursiveDescribeInternal(entity.AbsoluteUri, this, triples);

            return new BasicGraph(triples);
        }

        private static void RecursiveDescribeInternal(string subject, BasicGraph graph, HashSet<Triple> triples)
        {
            bool needsRecurse = false;

            var children = graph.SelectSubject(subject).Triples;

            foreach (var triple in children)
            {
                if (triples.Add(triple))
                {
                    needsRecurse = true;
                }
            }

            if (needsRecurse)
            {
                foreach (var triple in children)
                {
                    RecursiveDescribeInternal(triple.Object.GetValue(), graph, triples);
                }
            }
        }

        public HashSet<Triple> Triples
        {
            get
            {
                return _triples;
            }
        }

        public string NQuads
        {
            get
            {
                StringBuilder builder = new StringBuilder();

                foreach(var triple in Triples)
                {
                    builder.Append(FormatQuadNode(triple.Subject) + " ");
                    builder.Append(FormatQuadNode(triple.Predicate) + " ");
                    builder.Append(FormatQuadNode(triple.Object) + " ." + Environment.NewLine);
                }

                return builder.ToString();
            }
        }

        private static string FormatQuadNode(Node node)
        {
            if (node.IsIRI())
            {
                return String.Format(CultureInfo.InvariantCulture, "<{0}>", node.GetValue());
            }
            else if (node.IsLiteral())
            {
                return String.Format(CultureInfo.InvariantCulture, "\"{0}\"", node.GetValue());
            }

            return node.GetValue();
        }


        // Query
        public BasicGraph SelectSubject(Uri subject)
        {
            return SelectSubject(subject.AbsoluteUri);
        }

        public BasicGraph SelectSubject(string subject)
        {
            var triples = Triples.Where(t => t.Subject.GetValue() == subject);

            return new BasicGraph(triples.ToList());
        }

        public BasicGraph SelectPredicate(Uri predicate)
        {
            return SelectPredicate(predicate.AbsoluteUri);
        }

        public BasicGraph SelectPredicate(string predicate)
        {
            var triples = Triples.Where(t => t.Subject.GetValue() == predicate);

            return new BasicGraph(triples.ToList());
        }

    }

    public class Triple : IEquatable<Triple>
    {
        private readonly RDFDataset.Node _subNode;
        private readonly RDFDataset.Node _predNode;
        private readonly RDFDataset.Node _objNode;

        public Triple(RDFDataset.Node subNode, RDFDataset.Node predNode, RDFDataset.Node objNode)
        {
            _subNode = subNode;
            _predNode = predNode;
            _objNode = objNode;
        }

        public RDFDataset.Node Subject
        {
            get
            {
                return _subNode;
            }
        }

        public RDFDataset.Node Predicate
        {
            get
            {
                return _predNode;
            }
        }

        public RDFDataset.Node Object
        {
            get
            {
                return _objNode;
            }
        }

        public bool Equals(Triple other)
        {
            return Subject.Equals(other.Subject) && Predicate.Equals(other.Predicate) && Object.Equals(other.Object);
        }
    }

    //public class Node : IEquatable<Node>
    //{
    //    private readonly string _value;

    //    public Node(string value)
    //    {
    //        _value = value;
    //    }

    //    public virtual string Value
    //    {
    //        get
    //        {
    //            return _value;
    //        }
    //    }

    //    public bool Equals(Node other)
    //    {
    //        return Value == other.Value;
    //    }
    //}

    //public class UriNode : Node
    //{
    //    private readonly Uri _uri;

    //    public UriNode(Uri uri)
    //        : base(uri.AbsoluteUri)
    //    {
    //        _uri = uri;
    //    }

    //    public virtual Uri Uri
    //    {
    //        get
    //        {
    //            return _uri;
    //        }
    //    }
    //}
}
