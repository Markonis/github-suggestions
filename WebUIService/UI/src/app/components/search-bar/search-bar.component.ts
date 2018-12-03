import { Component, OnInit, Output, EventEmitter } from '@angular/core';


@Component({
  selector: 'app-search-bar',
  templateUrl: './search-bar.component.html',
  styleUrls: ['./search-bar.component.less']
})
export class SearchBarComponent implements OnInit {

  query = '';
  @Output() performSearch: EventEmitter<string> = new EventEmitter<string>();
  @Output() autoComplete: EventEmitter<string> = new EventEmitter<string>();

  constructor() { }

  onKeyup(event: KeyboardEvent) {
    if (event.keyCode === 13 && this.query.length > 0) {
      this.performSearch.emit(this.query);
    } else {
      this.autoComplete.emit(this.query);
    }
  }

  ngOnInit() {
  }

}
