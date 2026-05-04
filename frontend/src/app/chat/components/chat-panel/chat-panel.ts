import {
  AfterViewChecked,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';

import { ChatApiService } from '../../services/chat-api.service';

/**
 * One entry in the chat transcript. The author distinguishes the
 * user's own messages from the assistant's replies. Errors are
 * rendered as assistant messages with the isError flag set so we
 * can style them differently without introducing a third author.
 */
interface ChatMessage {
  id: number;
  author: 'user' | 'assistant';
  text: string;
  isError?: boolean;
}

/**
 * Slide-in chat drawer. Holds an in-session transcript, a message
 * input, and a send button. POSTs each message through ChatApiService
 * and appends the reply when it arrives. The transcript resets when
 * the page reloads (signal-only state, no localStorage).
 *
 * Device mutations the LLM performs propagate via the existing SSE
 * stream and update the dashboard automatically — this component
 * does not coordinate with the device list at all.
 */
@Component({
  selector: 'aura-chat-panel',
  standalone: true,
  imports: [FormsModule, ButtonModule, InputTextModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside
      class="chat-panel"
      [class.is-open]="open()"
      [attr.aria-hidden]="!open()"
      role="dialog"
      aria-label="Smart home assistant"
    >
      <header class="chat-head">
        <div class="chat-titles">
          <h2 class="chat-title">Assistant</h2>
          <p class="chat-subtitle">Ask me to control your devices</p>
        </div>
        <button
          type="button"
          class="chat-close"
          aria-label="Close chat"
          (click)="onClose()"
        >
          <i class="pi pi-times"></i>
        </button>
      </header>

      <div class="chat-transcript" #transcript>
        @if (messages().length === 0) {
          <div class="chat-empty">
            <p>Try saying:</p>
            <ul>
              <li>"Turn off the bedroom lights"</li>
              <li>"Set the thermostat to 70"</li>
              <li>"Lock all the doors"</li>
            </ul>
          </div>
        } @else {
          @for (msg of messages(); track msg.id) {
            <div
              class="chat-message"
              [class.is-user]="msg.author === 'user'"
              [class.is-assistant]="msg.author === 'assistant'"
              [class.is-error]="msg.isError"
            >
              <p class="chat-message-text">{{ msg.text }}</p>
            </div>
          }
        }

        @if (sending()) {
          <div class="chat-message is-assistant is-loading">
            <span class="chat-typing">
              <span></span><span></span><span></span>
            </span>
          </div>
        }
      </div>

      <form class="chat-input-row" (submit)="onSubmit($event)">
        <input
          pInputText
          class="chat-input"
          [ngModel]="draft()"
          (ngModelChange)="onDraftChange($event)"
          name="chat-message"
          placeholder="Message the assistant…"
          autocomplete="off"
          [disabled]="sending()"
        />
        <p-button
          type="submit"
          icon="pi pi-send"
          severity="primary"
          aria-label="Send"
          [disabled]="!canSend()"
          [loading]="sending()"
        />
      </form>
    </aside>
  `,
  styleUrl: './chat-panel.scss',
})
export class ChatPanel implements AfterViewChecked {
  /* ─────────────── Dependencies ─────────────── */

  private readonly chatApi = inject(ChatApiService);

  /* ─────────────── Inputs / outputs ─────────────── */

  readonly open = input.required<boolean>();
  readonly close = output<void>();

  /* ─────────────── Internal state ─────────────── */

  protected readonly messages = signal<readonly ChatMessage[]>([]);
  protected readonly draft = signal('');
  protected readonly sending = signal(false);

  /**
   * Send is enabled only when there's actual content in the draft
   * and we're not already waiting on a reply.
   */
  protected readonly canSend = computed(() =>
    this.draft().trim().length > 0 && !this.sending()
  );

  /* ─────────────── Auto-scroll ─────────────── */

  private readonly transcriptEl = viewChild<ElementRef<HTMLDivElement>>('transcript');
  private lastScrolledLength = 0;

  /**
   * Pin the transcript to the bottom whenever new messages arrive.
   * AfterViewChecked is the right hook because we need the new DOM
   * to be measured before scrollHeight reflects the new content.
   * Tracks lastScrolledLength to avoid scroll thrash on every CD.
   */
  ngAfterViewChecked(): void {
    const el = this.transcriptEl()?.nativeElement;
    if (!el) return;

    const messageCount = this.messages().length;
    const isLoading = this.sending() ? 1 : 0;
    const total = messageCount + isLoading;

    if (total !== this.lastScrolledLength) {
      el.scrollTop = el.scrollHeight;
      this.lastScrolledLength = total;
    }
  }

  /* ─────────────── Event handlers ─────────────── */

  protected onDraftChange(next: string): void {
    this.draft.set(next);
  }

  protected onClose(): void {
    this.close.emit();
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    if (!this.canSend()) return;

    const text = this.draft().trim();
    this.appendMessage({ author: 'user', text });
    this.draft.set('');
    this.sending.set(true);

    this.chatApi.sendMessage(text).subscribe({
      next: (reply) => {
        this.appendMessage({ author: 'assistant', text: reply });
        this.sending.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.appendMessage({
          author: 'assistant',
          text: this.extractErrorMessage(err),
          isError: true,
        });
        this.sending.set(false);
      },
    });
  }

  /* ─────────────── Helpers ─────────────── */

  private appendMessage(partial: Omit<ChatMessage, 'id'>): void {
    this.messages.update(list => [
      ...list,
      { id: Date.now() + list.length, ...partial },
    ]);
  }

  private extractErrorMessage(err: HttpErrorResponse): string {
    const body = err.error;
    if (body && typeof body === 'object') {
      if (typeof body.detail === 'string' && body.detail.length > 0) {
        return body.detail;
      }
      if (typeof body.title === 'string' && body.title.length > 0) {
        return body.title;
      }
    }
    return 'Sorry, the assistant could not respond. Please try again.';
  }
}
