/* 
 * Makerel API
 *
 * This is part of Makerel API.
 *
 * The version of the OpenAPI document: 0.1.0
 * 
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using OpenAPIDateConverter = Koudenpa.Mackerel.Api.Client.OpenAPIDateConverter;

namespace Koudenpa.Mackerel.Api.Model
{
    /// <summary>
    /// HostResponse
    /// </summary>
    [DataContract]
    public partial class HostResponse :  IEquatable<HostResponse>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HostResponse" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected HostResponse() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="HostResponse" /> class.
        /// </summary>
        /// <param name="host">host (required).</param>
        public HostResponse(Host host = default(Host))
        {
            // to ensure "host" is required (not null)
            this.Host = host ?? throw new ArgumentNullException("host is a required property for HostResponse and cannot be null");;
        }
        
        /// <summary>
        /// Gets or Sets Host
        /// </summary>
        [DataMember(Name="host", EmitDefaultValue=false)]
        public Host Host { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class HostResponse {\n");
            sb.Append("  Host: ").Append(Host).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as HostResponse);
        }

        /// <summary>
        /// Returns true if HostResponse instances are equal
        /// </summary>
        /// <param name="input">Instance of HostResponse to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(HostResponse input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Host == input.Host ||
                    (this.Host != null &&
                    this.Host.Equals(input.Host))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.Host != null)
                    hashCode = hashCode * 59 + this.Host.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

}
