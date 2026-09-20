import { Component, computed, inject, input, resource } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CampaignsApiService } from '../../api/campaigns-api.service';
import { Campaign } from '../../models/campaign.model';
import { parseCampaignId } from '../../routing/campaign-id';
import { CampaignApiError } from '../../state/problem-details';

type WorkspaceKind = 'chat' | 'sources';

/** Route shell that loads campaign context before rendering unavailable workspace capabilities. */
@Component({
  imports: [RouterLink],
  selector: 'app-campaign-workspace-shell',
  templateUrl: './campaign-workspace-shell.component.html',
})
export class CampaignWorkspaceShellComponent {
  readonly workspaceKind = input.required<WorkspaceKind>();

  private readonly route = inject(ActivatedRoute);
  private readonly campaignsApi = inject(CampaignsApiService);
  private readonly paramMap = toSignal(this.route.paramMap, {
    initialValue: this.route.snapshot.paramMap,
  });

  protected readonly campaignId = computed(() =>
    parseCampaignId(this.paramMap().get('campaignId')),
  );
  protected readonly validCampaignId = computed(() => {
    const result = this.campaignId();
    return result.valid ? result.value : undefined;
  });
  protected readonly campaign = resource<Campaign, string | undefined>({
    params: () => this.validCampaignId(),
    loader: ({ params }) => this.campaignsApi.getCampaign(params),
  });
  protected readonly currentCampaign = computed(() => {
    const expectedId = this.validCampaignId();

    if (!expectedId || !this.campaign.hasValue()) {
      return undefined;
    }

    const campaign = this.campaign.value();
    return campaign.id === expectedId ? campaign : undefined;
  });

  protected readonly title = computed(() =>
    this.workspaceKind() === 'chat' ? 'Chat is not available yet' : 'Sources are not available yet',
  );
  protected readonly kicker = computed(() =>
    this.workspaceKind() === 'chat' ? 'Lorekeeper · Chat shell' : 'Lorekeeper · Sources shell',
  );
  protected readonly unavailableCopy = computed(() =>
    this.workspaceKind() === 'chat'
      ? 'AI chat, source-backed querying, retrieval, citations, conversation history, streaming, and draft persistence are unavailable in this slice.'
      : 'Ingestion, upload, paste, Notion import, source updates, removal, indexing, retry, progress, and source lifecycle actions are unavailable in this slice.',
  );

  protected errorKind(): 'invalid' | 'notFound' | 'recoverable' | null {
    if (!this.campaignId().valid) {
      return 'invalid';
    }

    const error = unwrapCampaignApiError(this.campaign.error());

    if (error?.kind === 'notFound') {
      return 'notFound';
    }

    if (error) {
      return 'recoverable';
    }

    return null;
  }
}

function unwrapCampaignApiError(error: unknown): Partial<CampaignApiError> | undefined {
  let current = error;

  for (let depth = 0; depth < 3; depth += 1) {
    if (!current || typeof current !== 'object') {
      return undefined;
    }

    if ('kind' in current) {
      return current as Partial<CampaignApiError>;
    }

    current = (current as { cause?: unknown }).cause;
  }

  return undefined;
}
