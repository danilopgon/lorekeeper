import { Component, DestroyRef, effect, inject, resource, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, from } from 'rxjs';
import { CampaignsApiService } from '../../api/campaigns-api.service';
import { Campaign } from '../../models/campaign.model';
import { CampaignApiError } from '../../state/problem-details';

/** Campaign selection and creation route for the Block 01 workspace entry point. */
@Component({
  imports: [FormsModule, RouterLink],
  selector: 'app-campaign-selection-page',
  styleUrl: './campaign-selection.page.css',
  templateUrl: './campaign-selection.page.html',
})
export class CampaignSelectionPage {
  private readonly campaignsApi = inject(CampaignsApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refreshAfterActiveLoad = signal(false);

  protected campaignName = '';
  protected readonly creating = signal(false);
  protected readonly nameError = signal<'campaign_name_invalid' | 'campaign_name_conflict' | null>(
    null,
  );
  protected readonly createRecoverableError = signal(false);
  protected readonly campaigns = resource<Campaign[], unknown>({
    loader: () => this.campaignsApi.listCampaigns(),
  });

  constructor() {
    effect(() => {
      if (this.refreshAfterActiveLoad() && !this.campaigns.isLoading()) {
        this.refreshAfterActiveLoad.set(false);
        this.campaigns.reload();
      }
    });
  }

  /** Creates a campaign without selecting or navigating to it implicitly. */
  protected createCampaign(): void {
    if (this.creating()) {
      return;
    }

    const name = this.campaignName;
    this.startCreate();

    from(this.campaignsApi.createCampaign(name))
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.creating.set(false)),
      )
      .subscribe({
        next: () => this.handleCreateSuccess(),
        error: (error: unknown) => this.handleCreateError(error),
      });
  }

  /** Maps adapter field errors to English form copy. */
  protected fieldErrorMessage(code: 'campaign_name_invalid' | 'campaign_name_conflict'): string {
    return code === 'campaign_name_conflict'
      ? 'A campaign with this name already exists.'
      : 'Enter a campaign name between 1 and 120 characters.';
  }

  private startCreate(): void {
    this.creating.set(true);
    this.nameError.set(null);
    this.createRecoverableError.set(false);
  }

  private handleCreateSuccess(): void {
    this.campaignName = '';

    if (this.campaigns.isLoading()) {
      this.refreshAfterActiveLoad.set(true);
      return;
    }

    this.campaigns.reload();
  }

  private handleCreateError(error: unknown): void {
    const apiError = error as Partial<CampaignApiError>;

    if (apiError.kind === 'fieldError' && apiError.field === 'name') {
      this.nameError.set(apiError.code as 'campaign_name_invalid' | 'campaign_name_conflict');
      return;
    }

    this.createRecoverableError.set(true);
  }
}
