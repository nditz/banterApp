import { organizationSchema, websiteSchema } from "@/lib/seo.config";

type JsonLdProps = {
  data: Record<string, unknown>;
};

export function JsonLd({ data }: JsonLdProps) {
  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify(data) }}
    />
  );
}

export function OrganizationJsonLd() {
  return <JsonLd data={organizationSchema()} />;
}

export function WebsiteJsonLd() {
  return <JsonLd data={websiteSchema()} />;
}
