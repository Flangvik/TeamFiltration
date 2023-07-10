using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamFiltration.Models.MSOL
{


    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://schemas.xmlsoap.org/soap/envelope/", IsNullable = false)]
    public partial class Envelope
    {

        private EnvelopeHeader headerField;

        private EnvelopeBody bodyField;

        /// <remarks/>
        public EnvelopeHeader Header
        {
            get
            {
                return this.headerField;
            }
            set
            {
                this.headerField = value;
            }
        }

        /// <remarks/>
        public EnvelopeBody Body
        {
            get
            {
                return this.bodyField;
            }
            set
            {
                this.bodyField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public partial class EnvelopeHeader
    {

        private Action actionField;

        private ServerVersionInfo serverVersionInfoField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.w3.org/2005/08/addressing")]
        public Action Action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://schemas.microsoft.com/exchange/2010/Autodiscover")]
        public ServerVersionInfo ServerVersionInfo
        {
            get
            {
                return this.serverVersionInfoField;
            }
            set
            {
                this.serverVersionInfoField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.w3.org/2005/08/addressing")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.w3.org/2005/08/addressing", IsNullable = false)]
    public partial class Action
    {

        private byte mustUnderstandField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public byte mustUnderstand
        {
            get
            {
                return this.mustUnderstandField;
            }
            set
            {
                this.mustUnderstandField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/exchange/2010/Autodiscover")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://schemas.microsoft.com/exchange/2010/Autodiscover", IsNullable = false)]
    public partial class ServerVersionInfo
    {

        private byte majorVersionField;

        private byte minorVersionField;

        private ushort majorBuildNumberField;

        private byte minorBuildNumberField;

        private string versionField;

        /// <remarks/>
        public byte MajorVersion
        {
            get
            {
                return this.majorVersionField;
            }
            set
            {
                this.majorVersionField = value;
            }
        }

        /// <remarks/>
        public byte MinorVersion
        {
            get
            {
                return this.minorVersionField;
            }
            set
            {
                this.minorVersionField = value;
            }
        }

        /// <remarks/>
        public ushort MajorBuildNumber
        {
            get
            {
                return this.majorBuildNumberField;
            }
            set
            {
                this.majorBuildNumberField = value;
            }
        }

        /// <remarks/>
        public byte MinorBuildNumber
        {
            get
            {
                return this.minorBuildNumberField;
            }
            set
            {
                this.minorBuildNumberField = value;
            }
        }

        /// <remarks/>
        public string Version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public partial class EnvelopeBody
    {

        private GetFederationInformationResponseMessage getFederationInformationResponseMessageField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://schemas.microsoft.com/exchange/2010/Autodiscover")]
        public GetFederationInformationResponseMessage GetFederationInformationResponseMessage
        {
            get
            {
                return this.getFederationInformationResponseMessageField;
            }
            set
            {
                this.getFederationInformationResponseMessageField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/exchange/2010/Autodiscover")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://schemas.microsoft.com/exchange/2010/Autodiscover", IsNullable = false)]
    public partial class GetFederationInformationResponseMessage
    {

        private GetFederationInformationResponseMessageResponse responseField;

        /// <remarks/>
        public GetFederationInformationResponseMessageResponse Response
        {
            get
            {
                return this.responseField;
            }
            set
            {
                this.responseField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/exchange/2010/Autodiscover")]
    public partial class GetFederationInformationResponseMessageResponse
    {

        private string errorCodeField;

        private object errorMessageField;

        private string applicationUriField;

        private string[] domainsField;

        private GetFederationInformationResponseMessageResponseTokenIssuers tokenIssuersField;

        /// <remarks/>
        public string ErrorCode
        {
            get
            {
                return this.errorCodeField;
            }
            set
            {
                this.errorCodeField = value;
            }
        }

        /// <remarks/>
        public object ErrorMessage
        {
            get
            {
                return this.errorMessageField;
            }
            set
            {
                this.errorMessageField = value;
            }
        }

        /// <remarks/>
        public string ApplicationUri
        {
            get
            {
                return this.applicationUriField;
            }
            set
            {
                this.applicationUriField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Domain", IsNullable = false)]
        public string[] Domains
        {
            get
            {
                return this.domainsField;
            }
            set
            {
                this.domainsField = value;
            }
        }

        /// <remarks/>
        public GetFederationInformationResponseMessageResponseTokenIssuers TokenIssuers
        {
            get
            {
                return this.tokenIssuersField;
            }
            set
            {
                this.tokenIssuersField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/exchange/2010/Autodiscover")]
    public partial class GetFederationInformationResponseMessageResponseTokenIssuers
    {

        private GetFederationInformationResponseMessageResponseTokenIssuersTokenIssuer tokenIssuerField;

        /// <remarks/>
        public GetFederationInformationResponseMessageResponseTokenIssuersTokenIssuer TokenIssuer
        {
            get
            {
                return this.tokenIssuerField;
            }
            set
            {
                this.tokenIssuerField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/exchange/2010/Autodiscover")]
    public partial class GetFederationInformationResponseMessageResponseTokenIssuersTokenIssuer
    {

        private string endpointField;

        private string uriField;

        /// <remarks/>
        public string Endpoint
        {
            get
            {
                return this.endpointField;
            }
            set
            {
                this.endpointField = value;
            }
        }

        /// <remarks/>
        public string Uri
        {
            get
            {
                return this.uriField;
            }
            set
            {
                this.uriField = value;
            }
        }
    }


  
}
