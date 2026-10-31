import { formatCampaignDate } from './campaign-date';

describe('formatCampaignDate', () => {
  it('formats server timestamps as short Spanish dates in UTC', () => {
    expect(formatCampaignDate('2026-10-29T16:05:18.143494+00:00')).toBe('29 oct 2026');
  });

  it('does not shift the calendar day for late UTC timestamps', () => {
    expect(formatCampaignDate('2026-09-15T23:59:00Z')).toBe('15 sept 2026');
  });
});
