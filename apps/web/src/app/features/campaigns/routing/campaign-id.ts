const canonicalCampaignIdPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Campaign route id parsing result. */
export type CampaignIdParseResult =
  { valid: true; value: string } | { valid: false; code: 'campaign_id_invalid' };

/** Parses a route campaign id without accepting surrounding whitespace. */
export function parseCampaignId(value: string | null | undefined): CampaignIdParseResult {
  if (!value || !canonicalCampaignIdPattern.test(value)) {
    return { valid: false, code: 'campaign_id_invalid' };
  }

  return { valid: true, value: value.toLowerCase() };
}
