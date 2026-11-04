import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ChatShellPage } from './chat-shell.page';

describe('ChatShellPage', () => {
  let fixture: ComponentFixture<ChatShellPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatShellPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: route('123e4567-e89b-12d3-a456-426614174000'),
        },
      ],
    }).compileComponents();
  });

  it('renders the Chat unavailable route shell', async () => {
    fixture = TestBed.createComponent(ChatShellPage);

    fixture.detectChanges();
    TestBed.tick();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/campaigns/123e4567-e89b-12d3-a456-426614174000')
      .flush({
        id: '123e4567-e89b-12d3-a456-426614174000',
        name: 'Ash Crown',
        createdAt: '2026-09-15T10:00:00Z',
        updatedAt: '2026-09-15T10:00:00Z',
      });
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'El chat aún no está disponible',
    );
  });
});

function route(campaignId: string) {
  const paramMap = convertToParamMap({ campaignId });
  return { paramMap: of(paramMap), snapshot: { paramMap } };
}
