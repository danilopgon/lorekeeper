const canonicalCampaignIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;

export type CampaignIdParseResult =
  { valid: true; value: string } | { valid: false; code: 'campaign_id_invalid' };

export function parseCampaignId(value: string | null | undefined): CampaignIdParseResult {
  const candidate = value?.trim().toLowerCase();

  if (!candidate || !canonicalCampaignIdPattern.test(candidate)) {
    return { valid: false, code: 'campaign_id_invalid' };
  }

  return { valid: true, value: candidate };
}
