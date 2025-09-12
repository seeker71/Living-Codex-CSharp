#!/usr/bin/env python3
"""
Detailed Route Analysis Tool
Analyzes each route to determine:
1. Where it belongs (current module vs. better module)
2. If the route name is appropriate
3. If routes should be moved
4. What references need updating
"""

import json
import sys
from collections import defaultdict
from typing import Dict, List, Tuple, Any

def analyze_route_placement(route_data: Dict[str, Any]) -> Dict[str, Any]:
    """Analyze where a route should belong based on its functionality"""
    
    route = route_data['route']
    api_name = route_data['apiName']
    module_id = route_data['moduleId']
    verb = route_data['verb']
    
    analysis = {
        'current_module': module_id,
        'suggested_module': module_id,  # Default to current
        'should_move': False,
        'reason': '',
        'route_appropriate': True,
        'route_suggestion': route,
        'issues': []
    }
    
    # Analyze based on route patterns and functionality
    if route.startswith('/ai/') or 'ai' in api_name.lower():
        if not module_id.startswith('ai'):
            analysis['suggested_module'] = 'ai-analysis'
            analysis['should_move'] = True
            analysis['reason'] = 'AI-related functionality should be in AI module'
            analysis['issues'].append('AI route in non-AI module')
    
    elif route.startswith('/translation/') or 'translate' in api_name.lower():
        if not module_id.startswith('translation'):
            analysis['suggested_module'] = 'translation'
            analysis['should_move'] = True
            analysis['reason'] = 'Translation functionality should be in Translation module'
            analysis['issues'].append('Translation route in non-translation module')
    
    elif route.startswith('/concept/') or 'concept' in api_name.lower():
        if not module_id.startswith('concept'):
            analysis['suggested_module'] = 'codex.concept'
            analysis['should_move'] = True
            analysis['reason'] = 'Concept functionality should be in Concept module'
            analysis['issues'].append('Concept route in non-concept module')
    
    elif route.startswith('/llm/') or 'llm' in api_name.lower():
        if not module_id.startswith('ai'):
            analysis['suggested_module'] = 'ai-analysis'
            analysis['should_move'] = True
            analysis['reason'] = 'LLM functionality should be in AI module'
            analysis['issues'].append('LLM route in non-AI module')
    
    elif route.startswith('/joy/') or 'joy' in api_name.lower():
        if not module_id.startswith('joy'):
            analysis['suggested_module'] = 'codex.joy'
            analysis['should_move'] = True
            analysis['reason'] = 'Joy functionality should be consolidated'
            analysis['issues'].append('Joy route scattered across modules')
    
    elif route.startswith('/resonance/') or 'resonance' in api_name.lower():
        if not module_id.startswith('resonance'):
            analysis['suggested_module'] = 'codex.resonance'
            analysis['should_move'] = True
            analysis['reason'] = 'Resonance functionality should be consolidated'
            analysis['issues'].append('Resonance route scattered across modules')
    
    elif route.startswith('/storage/') and not route.startswith('/storage-endpoints/'):
        if module_id != 'codex.storage':
            analysis['suggested_module'] = 'codex.storage'
            analysis['should_move'] = True
            analysis['reason'] = 'Storage functionality should be in Storage module'
            analysis['issues'].append('Storage route in non-storage module')
    
    # Check for inappropriate route names
    if 'llm-' in api_name and not route.startswith('/llm/'):
        analysis['route_appropriate'] = False
        analysis['route_suggestion'] = route.replace('/ai/', '/llm/') if route.startswith('/ai/') else f'/llm{route}'
        analysis['issues'].append('LLM route not under /llm/ path')
    
    if 'translate' in api_name and not route.startswith('/translation/'):
        analysis['route_appropriate'] = False
        analysis['route_suggestion'] = route.replace('/llm/', '/translation/') if route.startswith('/llm/') else f'/translation{route}'
        analysis['issues'].append('Translation route not under /translation/ path')
    
    # Check for duplicate functionality
    if 'translate' in api_name and 'llm' in module_id:
        analysis['issues'].append('Translation functionality in LLM module - should be separate')
    
    return analysis

def analyze_module_cohesion(modules: Dict[str, List[Dict]]) -> Dict[str, Any]:
    """Analyze how well routes fit within their modules"""
    
    module_analysis = {}
    
    for module_id, routes in modules.items():
        issues = []
        suggested_consolidations = []
        
        # Check for mixed concerns
        route_types = set()
        for route in routes:
            if route['route'].startswith('/ai/') or 'ai' in route['apiName'].lower():
                route_types.add('ai')
            elif route['route'].startswith('/translation/') or 'translate' in route['apiName'].lower():
                route_types.add('translation')
            elif route['route'].startswith('/concept/') or 'concept' in route['apiName'].lower():
                route_types.add('concept')
            elif route['route'].startswith('/joy/') or 'joy' in route['apiName'].lower():
                route_types.add('joy')
            elif route['route'].startswith('/resonance/') or 'resonance' in route['apiName'].lower():
                route_types.add('resonance')
            elif route['route'].startswith('/storage/') or 'storage' in route['apiName'].lower():
                route_types.add('storage')
        
        if len(route_types) > 2:
            issues.append(f'Module handles multiple concerns: {", ".join(route_types)}')
        
        # Check for scattered functionality
        if 'joy' in route_types and module_id != 'codex.joy':
            issues.append('Joy functionality scattered across modules')
            suggested_consolidations.append('Consolidate joy routes into codex.joy module')
        
        if 'resonance' in route_types and module_id != 'codex.resonance':
            issues.append('Resonance functionality scattered across modules')
            suggested_consolidations.append('Consolidate resonance routes into codex.resonance module')
        
        module_analysis[module_id] = {
            'route_count': len(routes),
            'concerns': list(route_types),
            'issues': issues,
            'suggested_consolidations': suggested_consolidations,
            'cohesion_score': max(0, 10 - len(issues) - len(route_types))
        }
    
    return module_analysis

def main():
    # Read the route data from stdin
    try:
        route_data = json.load(sys.stdin)
    except json.JSONDecodeError as e:
        print(f"Error parsing JSON: {e}", file=sys.stderr)
        sys.exit(1)
    
    # Group routes by module
    modules = defaultdict(list)
    for route in route_data:
        modules[route['moduleId']].append(route)
    
    # Analyze each route
    route_analyses = []
    for route in route_data:
        analysis = analyze_route_placement(route)
        analysis.update(route)
        route_analyses.append(analysis)
    
    # Analyze module cohesion
    module_analysis = analyze_module_cohesion(modules)
    
    # Generate report
    print("=" * 80)
    print("DETAILED ROUTE ANALYSIS REPORT")
    print("=" * 80)
    
    # Routes that should be moved
    routes_to_move = [r for r in route_analyses if r['should_move']]
    print(f"\nROUTES THAT SHOULD BE MOVED ({len(routes_to_move)}):")
    print("-" * 50)
    
    for route in routes_to_move:
        print(f"Route: {route['route']}")
        print(f"  Current Module: {route['current_module']}")
        print(f"  Suggested Module: {route['suggested_module']}")
        print(f"  Reason: {route['reason']}")
        print(f"  Issues: {', '.join(route['issues'])}")
        print()
    
    # Routes with inappropriate names
    inappropriate_routes = [r for r in route_analyses if not r['route_appropriate']]
    print(f"\nROUTES WITH INAPPROPRIATE NAMES ({len(inappropriate_routes)}):")
    print("-" * 50)
    
    for route in inappropriate_routes:
        print(f"Route: {route['route']}")
        print(f"  Current Name: {route['apiName']}")
        print(f"  Suggested Route: {route['route_suggestion']}")
        print(f"  Issues: {', '.join(route['issues'])}")
        print()
    
    # Module cohesion analysis
    print(f"\nMODULE COHESION ANALYSIS:")
    print("-" * 50)
    
    for module_id, analysis in sorted(module_analysis.items()):
        print(f"\nModule: {module_id}")
        print(f"  Route Count: {analysis['route_count']}")
        print(f"  Concerns: {', '.join(analysis['concerns']) if analysis['concerns'] else 'None'}")
        print(f"  Cohesion Score: {analysis['cohesion_score']}/10")
        if analysis['issues']:
            print(f"  Issues: {', '.join(analysis['issues'])}")
        if analysis['suggested_consolidations']:
            print(f"  Suggestions: {', '.join(analysis['suggested_consolidations'])}")
    
    # Summary statistics
    print(f"\nSUMMARY STATISTICS:")
    print("-" * 50)
    print(f"Total Routes: {len(route_data)}")
    print(f"Routes to Move: {len(routes_to_move)}")
    print(f"Inappropriate Names: {len(inappropriate_routes)}")
    print(f"Total Issues: {sum(len(r['issues']) for r in route_analyses)}")
    
    # Save detailed analysis to file
    with open('detailed_route_analysis.json', 'w') as f:
        json.dump({
            'route_analyses': route_analyses,
            'module_analysis': module_analysis,
            'summary': {
                'total_routes': len(route_data),
                'routes_to_move': len(routes_to_move),
                'inappropriate_names': len(inappropriate_routes),
                'total_issues': sum(len(r['issues']) for r in route_analyses)
            }
        }, f, indent=2)
    
    print(f"\nDetailed analysis saved to: detailed_route_analysis.json")

if __name__ == "__main__":
    main()
