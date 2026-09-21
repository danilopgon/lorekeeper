import { parseCampaignId } from './campaign-id';

describe('parseCampaignId', () => {
  it('accepts canonical campaign UUIDs', () => {
    expect(parseCampaignId('123e4567-e89b-12d3-a456-426614174000')).toEqual({
      valid: true,
      value: '123e4567-e89b-12d3-a456-426614174000',
    });
  });

  it('normalizes uppercase UUID input for route use', () => {
    expect(parseCampaignId('123E4567-E89B-12D3-A456-426614174000')).toEqual({
      valid: true,
      value: '123e4567-e89b-12d3-a456-426614174000',
    });
  });

  it('rejects malformed UUIDs and whitespace-wrapped route values', () => {
    expect(parseCampaignId('not-a-guid')).toEqual({ valid: false, code: 'campaign_id_invalid' });
    expect(parseCampaignId(' 123e4567-e89b-12d3-a456-426614174000 ')).toEqual({
      valid: false,
      code: 'campaign_id_invalid',
    });
    expect(parseCampaignId(null)).toEqual({ valid: false, code: 'campaign_id_invalid' });
  });
});
