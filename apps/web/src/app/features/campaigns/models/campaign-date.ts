const campaignDateFormat = new Intl.DateTimeFormat('es', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  timeZone: 'UTC',
});

/** Formats a server UTC timestamp as a short Spanish calendar date, e.g. `29 oct 2026`. */
export function formatCampaignDate(isoTimestamp: string): string {
  return campaignDateFormat.format(new Date(isoTimestamp)).replace('.', '');
}
