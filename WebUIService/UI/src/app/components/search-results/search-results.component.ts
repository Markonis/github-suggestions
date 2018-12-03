import { Component, OnInit, Input } from '@angular/core';
import { SearchOutput } from 'src/app/domain/messages/search/search-output';

@Component({
  selector: 'app-search-results',
  templateUrl: './search-results.component.html',
  styleUrls: ['./search-results.component.less']
})
export class SearchResultsComponent implements OnInit {

  @Input() searchOutput: SearchOutput = null;
  @Input() suggestOutput: SearchOutput = null;

  constructor() { }

  ngOnInit() {
  }

}
