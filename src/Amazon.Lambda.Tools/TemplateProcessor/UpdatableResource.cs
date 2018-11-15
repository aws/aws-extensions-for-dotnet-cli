using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Amazon.Lambda.Tools.TemplateProcessor
{

    /// <summary>
    /// The updatable resource like a CloudFormation AWS::Lambda::Function. This class combines the UpdatableResourceDefinition
    /// which identifies the fields that can be updated and IUpdatableResourceDataSource which abstracts the JSON or YAML definition.
    /// </summary>
    public class UpdatableResource : IUpdatableResource
    {
        public string Name { get; }
        public string ResourceType { get; }
        public IList<IUpdateResourceField> Fields { get; }
        
        UpdatableResourceDefinition Definition { get; } 
        IUpdatableResourceDataSource DataSource { get; }

        public UpdatableResource(string name, UpdatableResourceDefinition definition, IUpdatableResourceDataSource dataSource)
        {
            this.Name = name;
            this.Definition = definition;
            this.DataSource = dataSource;
            
            this.Fields = new List<IUpdateResourceField>();
            foreach (var fieldDefinition in definition.Fields)
            {
                this.Fields.Add(new UpdatableResourceField(this, fieldDefinition));
            }
        }

        public string LambdaRuntime
        {
            get
            {
                var runtime = this.DataSource.GetValue("Runtime");
                if(string.IsNullOrEmpty(runtime))
                {
                    runtime = this.DataSource.GetValueFromRoot("Globals", "Function", "Runtime");
                }

                return runtime;
            }
        }

        public class UpdatableResourceField : IUpdateResourceField
        {
            public IUpdatableResource Resource => this._resource;
            public UpdatableResource _resource;

            private UpdatableResourceDefinition.FieldDefinition Field { get; }

            public UpdatableResourceField(UpdatableResource resource, UpdatableResourceDefinition.FieldDefinition field)
            {
                this._resource = resource;
                this.Field = field;
            }

            public string Name => this.Field.Name;

            public bool IsCode => this.Field.IsCode;

            public string GetLocalPath()
            {
                return this.Field.GetLocalPath(this._resource.DataSource);
            }

            public void SetS3Location(string s3Bucket, string s3Key)
            {
                this.Field.SetS3Location(this._resource.DataSource, s3Bucket, s3Key);
            }
        }
    }
}
