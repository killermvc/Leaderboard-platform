import { Component, signal, input, output } from '@angular/core';

export interface DropdownElement {
  value: string;
  label: string;
  icon?: string;
}

@Component({
  selector: 'app-dropdown-menu',
  imports: [],
  templateUrl: './dropdown-menu.html',
  styleUrl: './dropdown-menu.scss',
})
export class DropdownMenu {
	menuOpen = signal(false);
	elements = input<DropdownElement[]>([]);
	selectedValue = input<string>();
	onSelect = output<string>();

	toggleMenu() {
		this.menuOpen.update((current) => !current);
	}

	isMenuOpen(): boolean
	{
		return this.menuOpen();
	}

	selectElement(element: DropdownElement) {
		this.onSelect.emit(element.value);
		this.menuOpen.set(false);
	}

	getSelectedElement(): DropdownElement | undefined {
		const elements = this.elements();
		return elements.find(element => element.value === this.selectedValue()) ?? elements[0];
	}
}
