import { Component, OnInit, Input } from '@angular/core';
import { Item } from 'src/app/domain/messages/search/search-output';

@Component({
  selector: 'app-search-result',
  templateUrl: './search-result.component.html',
  styleUrls: ['./search-result.component.less']
})
export class SearchResultComponent implements OnInit {

  @Input() item: Item = null;
  @Input() type: 'result' | 'suggestion' = 'result';

  constructor() { }

  ngOnInit() {
  }

}
