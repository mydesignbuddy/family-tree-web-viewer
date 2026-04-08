import {
  Component, Input, Output, EventEmitter,
  ElementRef, OnChanges, OnDestroy, SimpleChanges, AfterViewInit
} from '@angular/core';
import * as d3 from 'd3';
import { FamilyTreeData, Individual, DescendantNode } from '../../models/family-tree.model';
import { TreeDataService } from '../../services/tree-data.service';

interface HierarchyNode {
  person: Individual;
  spouse?: Individual;
  children?: HierarchyNode[];
}

@Component({
  selector: 'app-descendant-tree',
  standalone: true,
  template: '<div class="descendant-container"></div>',
  styles: [`
    :host { display: block; width: 100%; height: 100%; }
    .descendant-container { width: 100%; height: 100%; }
  `]
})
export class DescendantTreeComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() data!: FamilyTreeData;
  @Input() rootPersonId!: string;
  @Output() personClicked = new EventEmitter<Individual>();

  private svg: any;
  private zoomGroup: any;
  private resizeObserver?: ResizeObserver;

  private readonly CARD_WIDTH = 160;
  private readonly CARD_HEIGHT = 64;
  private readonly COUPLE_GAP = 12;
  private readonly COUPLE_BLOCK_WIDTH = 160 * 2 + 12;

  constructor(
    private el: ElementRef,
    private treeDataService: TreeDataService
  ) {}

  ngAfterViewInit(): void {
    this.initSvg();
    this.render();
    this.resizeObserver = new ResizeObserver(() => this.render());
    this.resizeObserver.observe(this.el.nativeElement);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.svg && (changes['rootPersonId'] || changes['data'])) {
      this.render();
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  private initSvg(): void {
    const container = this.el.nativeElement.querySelector('.descendant-container');
    this.svg = d3.select(container)
      .append('svg')
      .attr('width', '100%')
      .attr('height', '100%');

    this.zoomGroup = this.svg.append('g');

    const zoom = d3.zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.1, 3])
      .on('zoom', (event: d3.D3ZoomEvent<SVGSVGElement, unknown>) => {
        this.zoomGroup.attr('transform', event.transform);
      });

    this.svg.call(zoom);
  }

  private render(): void {
    if (!this.data || !this.rootPersonId) return;

    this.zoomGroup.selectAll('*').remove();

    const descendantTree = this.treeDataService.buildDescendantTree(this.rootPersonId, this.data);
    if (!descendantTree) return;

    const hierarchyData = this.convertToHierarchy(descendantTree);
    const root = d3.hierarchy(hierarchyData);

    const treeLayout = d3.tree<HierarchyNode>()
      .nodeSize([this.COUPLE_BLOCK_WIDTH + 40, 140])
      .separation((a, b) => a.parent === b.parent ? 1 : 1.15);

    treeLayout(root);

    const container = this.el.nativeElement.querySelector('.descendant-container');
    const width = container.clientWidth;
    const height = container.clientHeight;

    // Draw links (orthogonal connectors)
    this.zoomGroup.selectAll('.link')
      .data(root.links())
      .join('path')
      .attr('class', 'link')
      .attr('d', (d: d3.HierarchyLink<HierarchyNode>) => {
        const sx = (d.source as any).x + width / 2;
        const sy = (d.source as any).y + 100;
        const tx = (d.target as any).x + width / 2;
        const ty = (d.target as any).y + 100;
        const my = sy + (ty - sy) / 2;
        return `M${sx},${sy + this.CARD_HEIGHT / 2} L${sx},${my} L${tx},${my} L${tx},${ty - this.CARD_HEIGHT / 2}`;
      })
      .attr('fill', 'none')
      .attr('stroke', '#c8c4bc')
      .attr('stroke-width', 2);

    // Draw nodes
    const nodes = this.zoomGroup.selectAll('.node')
      .data(root.descendants())
      .join('g')
      .attr('class', 'node')
      .attr('transform', (d: d3.HierarchyPointNode<HierarchyNode>) =>
        `translate(${d.x + width / 2},${d.y + 100})`);

    // For each node, draw the person card and optionally the spouse card
    nodes.each((d: d3.HierarchyPointNode<HierarchyNode>, i: number, nodeElements: SVGGElement[] | ArrayLike<SVGGElement>) => {
      const g = d3.select(nodeElements[i]);
      const hasSpouse = !!d.data.spouse;

      // Person card (left or center)
      const personX = hasSpouse ? -(this.CARD_WIDTH + this.COUPLE_GAP / 2) : -this.CARD_WIDTH / 2;
      this.drawCard(g, d.data.person, personX, -this.CARD_HEIGHT / 2, d.data.person.id === this.rootPersonId);

      if (hasSpouse && d.data.spouse) {
        // Spouse card (right)
        const spouseX = this.COUPLE_GAP / 2;
        this.drawCard(g, d.data.spouse, spouseX, -this.CARD_HEIGHT / 2, false);

        // Marriage connector line
        g.append('line')
          .attr('x1', personX + this.CARD_WIDTH)
          .attr('y1', 0)
          .attr('x2', spouseX)
          .attr('y2', 0)
          .attr('stroke', '#e74c3c')
          .attr('stroke-width', 2)
          .attr('stroke-dasharray', '4,2');
      }
    });
  }

  private drawCard(parent: any, person: Individual, x: number, y: number, isRoot: boolean): void {
    const card = parent.append('g')
      .attr('transform', `translate(${x},${y})`)
      .style('cursor', 'pointer')
      .on('click', () => this.personClicked.emit(person));

    // Background
    card.append('rect')
      .attr('width', this.CARD_WIDTH)
      .attr('height', this.CARD_HEIGHT)
      .attr('rx', 8)
      .attr('ry', 8)
      .attr('fill', '#ffffff')
      .attr('stroke', isRoot ? '#2a7d3f' : '#e0ddd5')
      .attr('stroke-width', isRoot ? 2.5 : 1.5)
      .attr('filter', 'drop-shadow(0 2px 4px rgba(0,0,0,0.08))');

    // Gender bar
    card.append('rect')
      .attr('width', 4)
      .attr('height', this.CARD_HEIGHT - 14)
      .attr('x', 7)
      .attr('y', 7)
      .attr('rx', 2)
      .attr('fill', person.sex === 'M' ? '#4a90d9' : person.sex === 'F' ? '#d94a8e' : '#95a5a6');

    // Name
    card.append('text')
      .attr('x', 18)
      .attr('y', 25)
      .attr('font-size', '12px')
      .attr('font-weight', '600')
      .attr('fill', '#2c3e50')
      .attr('font-family', "'Inter', sans-serif")
      .text(this.treeDataService.getDisplayName(person))
      .each(function(this: SVGTextElement) {
        const textEl = d3.select(this);
        if (this.getComputedTextLength() > 130) {
          let text = textEl.text();
          while (this.getComputedTextLength() > 122 && text.length > 0) {
            text = text.slice(0, -1);
            textEl.text(text + '...');
          }
        }
      });

    // Lifespan
    card.append('text')
      .attr('x', 18)
      .attr('y', 42)
      .attr('font-size', '10px')
      .attr('fill', '#7f8c8d')
      .attr('font-family', "'Inter', sans-serif")
      .text(this.treeDataService.getLifespan(person));

    // Hover
    card.on('mouseenter', function(this: SVGGElement) {
      d3.select(this).select('rect:first-child')
        .transition().duration(200)
        .attr('filter', 'drop-shadow(0 4px 12px rgba(0,0,0,0.15))');
    }).on('mouseleave', function(this: SVGGElement) {
      d3.select(this).select('rect:first-child')
        .transition().duration(200)
        .attr('filter', 'drop-shadow(0 2px 4px rgba(0,0,0,0.08))');
    });
  }

  private convertToHierarchy(node: DescendantNode): HierarchyNode {
    const result: HierarchyNode = {
      person: node.person,
      spouse: node.spouse
    };

    if (node.children.length > 0) {
      result.children = node.children.map(c => this.convertToHierarchy(c));
    }

    return result;
  }
}
