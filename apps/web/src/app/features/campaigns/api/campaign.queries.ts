import { httpResource, HttpResourceRef } from '@angular/common/http';
import { Signal } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { CampaignDto } from '../../../shared/api/generated';
import {
  zGetCampaignResponse,
  zListCampaignsResponse,
} from '../../../shared/api/generated/zod.gen';

const campaignsUrl = `${environment.apiBaseUrl}/api/campaigns`;

/** Reads every campaign; call from a component field initializer. */
export function injectCampaignListQuery(): HttpResourceRef<CampaignDto[] | undefined> {
  return httpResource(() => campaignsUrl, { parse: zListCampaignsResponse.parse });
}

/** Reads one campaign and sends no request while the id is undefined. */
export function injectCampaignQuery(
  campaignId: Signal<string | undefined>,
): HttpResourceRef<CampaignDto | undefined> {
  return httpResource(
    () => {
      const id = campaignId();
      return id === undefined ? undefined : `${campaignsUrl}/${encodeURIComponent(id)}`;
    },
    { parse: zGetCampaignResponse.parse },
  );
}
