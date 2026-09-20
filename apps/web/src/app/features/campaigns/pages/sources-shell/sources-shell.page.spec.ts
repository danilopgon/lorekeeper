import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { CampaignsApiService } from '../../api/campaigns-api.service';
import { Campaign } from '../../models/campaign.model';
import { SourcesShellPage } from './sources-shell.page';

describe('SourcesShellPage', () => {
  let fixture: ComponentFixture<SourcesShellPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SourcesShellPage],
      providers: [
        provideRouter([]),
        { provide: CampaignsApiService, useValue: new CampaignsApiStub() },
        {
          provide: ActivatedRoute,
          useValue: route('123e4567-e89b-12d3-a456-426614174000'),
        },
      ],
    }).compileComponents();
  });

  it('renders the Sources unavailable route shell', async () => {
    fixture = TestBed.createComponent(SourcesShellPage);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Sources are not available yet',
    );
  });
});

class CampaignsApiStub {
  async getCampaign(id: string): Promise<Campaign> {
    return {
      id,
      name: 'Ash Crown',
      createdAt: '2026-09-15T10:00:00Z',
      updatedAt: '2026-09-15T10:00:00Z',
    };
  }
}

function route(campaignId: string) {
  const paramMap = convertToParamMap({ campaignId });
  return { paramMap: of(paramMap), snapshot: { paramMap } };
}
