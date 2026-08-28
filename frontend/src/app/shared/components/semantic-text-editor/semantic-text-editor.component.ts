import { AfterViewInit, Component, ElementRef, ViewChild, forwardRef, input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/**
 * Semantic-only rich text: paragraph, heading, subheading, bold, italic, list, quote.
 * No fonts/colors/inline CSS — typography is owned by the frontend.
 */
@Component({
  selector: 'app-semantic-text-editor',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SemanticTextEditorComponent),
      multi: true
    }
  ],
  template: `
    <div class="border border-memorial-line bg-white">
      <div class="flex flex-wrap gap-1 border-b border-memorial-line p-2">
        <button type="button" class="px-2 py-1 text-xs hover:bg-[#F4F2EE]" (click)="format('formatBlock', 'P')" title="Абзац">¶</button>
        <button type="button" class="px-2 py-1 text-xs hover:bg-[#F4F2EE]" (click)="format('formatBlock', 'H2')" title="Заголовок">H2</button>
        <button type="button" class="px-2 py-1 text-xs hover:bg-[#F4F2EE]" (click)="format('formatBlock', 'H3')" title="Підзаголовок">H3</button>
        <button type="button" class="px-2 py-1 text-xs font-bold hover:bg-[#F4F2EE]" (click)="format('bold')" title="Жирний">B</button>
        <button type="button" class="px-2 py-1 text-xs italic hover:bg-[#F4F2EE]" (click)="format('italic')" title="Курсив">I</button>
        <button type="button" class="px-2 py-1 text-xs hover:bg-[#F4F2EE]" (click)="format('insertUnorderedList')" title="Список">• List</button>
        <button type="button" class="px-2 py-1 text-xs hover:bg-[#F4F2EE]" (click)="format('formatBlock', 'BLOCKQUOTE')" title="Цитата">«»</button>
      </div>
      <div
        #editor
        class="min-h-[140px] px-3 py-2 font-sans text-sm leading-relaxed outline-none"
        contenteditable="true"
        (input)="onInput()"
        (blur)="onTouched()"
      ></div>
    </div>
  `
})
export class SemanticTextEditorComponent implements ControlValueAccessor, AfterViewInit {
  readonly placeholder = input('Текст…');
  @ViewChild('editor') editorRef!: ElementRef<HTMLDivElement>;

  private onChange: (value: string) => void = () => undefined;
  onTouched: () => void = () => undefined;
  private pendingHtml = '<p></p>';
  private ready = false;

  ngAfterViewInit(): void {
    this.ready = true;
    this.editorRef.nativeElement.innerHTML = this.pendingHtml || '<p></p>';
  }

  writeValue(value: string | null): void {
    this.pendingHtml = value || '<p></p>';
    if (this.ready) {
      this.editorRef.nativeElement.innerHTML = this.pendingHtml;
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  format(command: string, value?: string): void {
    this.editorRef.nativeElement.focus();
    // execCommand is sufficient for semantic toolbar in this foundation editor
    document.execCommand(command, false, value);
    this.onInput();
  }

  onInput(): void {
    const html = this.editorRef.nativeElement.innerHTML;
    this.onChange(html);
  }
}
