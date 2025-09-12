#!/usr/bin/env python3
"""
Spec-Driven Module Conversion Plan
Identifies and prioritizes modules for hot-reload conversion to spec-driven architecture
"""

import requests
import json
from typing import List, Dict, Any
from dataclasses import dataclass
from datetime import datetime

@dataclass
class ModuleConversionCandidate:
    id: str
    name: str
    priority: int
    reason: str
    features: List[str]
    routes: int
    is_hot_reloadable: bool
    conversion_strategy: str

class SpecDrivenConversionPlanner:
    def __init__(self, base_url: str = "http://localhost:5001"):
        self.base_url = base_url
        self.session = requests.Session()
    
    def get_system_overview(self) -> Dict[str, Any]:
        """Get comprehensive system overview"""
        response = self.session.get(f"{self.base_url}/spec/status/overview")
        response.raise_for_status()
        return response.json()
    
    def get_all_modules(self) -> List[Dict[str, Any]]:
        """Get all modules with their details"""
        response = self.session.get(f"{self.base_url}/spec/modules/all")
        response.raise_for_status()
        return response.json()["modules"]
    
    def get_all_routes(self) -> List[Dict[str, Any]]:
        """Get all routes with their details"""
        response = self.session.get(f"{self.base_url}/spec/routes/all")
        response.raise_for_status()
        return response.json()["routes"]
    
    def get_features_map(self) -> List[Dict[str, Any]]:
        """Get features mapped to modules"""
        response = self.session.get(f"{self.base_url}/spec/features/map")
        response.raise_for_status()
        return response.json()["features"]
    
    def analyze_conversion_candidates(self) -> List[ModuleConversionCandidate]:
        """Analyze modules and identify conversion candidates"""
        modules = self.get_all_modules()
        routes = self.get_all_routes()
        features = self.get_features_map()
        
        # Create route count map
        route_counts = {}
        for route in routes:
            module_id = route.get("moduleId", "unknown")
            route_counts[module_id] = route_counts.get(module_id, 0) + 1
        
        candidates = []
        
        for module in modules:
            module_id = module["id"]
            name = module["name"]
            features_list = module.get("features", [])
            is_hot_reloadable = module.get("isHotReloadable", False)
            is_stable = module.get("isStable", False)
            route_count = route_counts.get(module_id, 0)
            
            # Skip stable modules
            if is_stable:
                continue
            
            # Calculate priority and determine conversion strategy
            priority, reason, strategy = self._calculate_conversion_priority(
                module_id, name, features_list, is_hot_reloadable, route_count
            )
            
            if priority > 0:  # Only include modules worth converting
                candidates.append(ModuleConversionCandidate(
                    id=module_id,
                    name=name,
                    priority=priority,
                    reason=reason,
                    features=features_list,
                    routes=route_count,
                    is_hot_reloadable=is_hot_reloadable,
                    conversion_strategy=strategy
                ))
        
        # Sort by priority (highest first)
        candidates.sort(key=lambda x: x.priority, reverse=True)
        return candidates
    
    def _calculate_conversion_priority(self, module_id: str, name: str, features: List[str], 
                                     is_hot_reloadable: bool, route_count: int) -> tuple[int, str, str]:
        """Calculate conversion priority for a module"""
        priority = 0
        reasons = []
        strategy = "standard"
        
        # Base priority factors
        if "AI" in features or "LLM" in features:
            priority += 20
            reasons.append("AI/LLM features")
            strategy = "ai-enhanced"
        
        if "Resonance" in features:
            priority += 15
            reasons.append("Resonance features")
            strategy = "resonance-optimized"
        
        if "Real-time" in features:
            priority += 12
            reasons.append("Real-time features")
            strategy = "realtime-optimized"
        
        if "Translation" in features:
            priority += 10
            reasons.append("Translation features")
            strategy = "translation-optimized"
        
        if "Security" in features:
            priority += 8
            reasons.append("Security features")
            strategy = "security-focused"
        
        if "Graph" in features:
            priority += 6
            reasons.append("Graph features")
            strategy = "graph-optimized"
        
        # Route complexity factor
        if route_count > 10:
            priority += 8
            reasons.append(f"High route count ({route_count})")
        elif route_count > 5:
            priority += 4
            reasons.append(f"Medium route count ({route_count})")
        
        # Hot-reload readiness
        if is_hot_reloadable:
            priority += 5
            reasons.append("Already hot-reloadable")
            strategy = "hot-reload-ready"
        
        # Module-specific priorities
        if "test" in module_id.lower() or "demo" in module_id.lower():
            priority += 3
            reasons.append("Test/Demo module")
            strategy = "test-optimized"
        
        if "spec" in module_id.lower():
            priority += 15
            reasons.append("Spec-related module")
            strategy = "spec-native"
        
        if "concept" in module_id.lower():
            priority += 12
            reasons.append("Concept management")
            strategy = "concept-optimized"
        
        # Negative factors
        if "core" in module_id.lower() and not is_hot_reloadable:
            priority -= 5
            reasons.append("Core module - lower priority")
        
        if route_count == 0:
            priority -= 3
            reasons.append("No routes - limited functionality")
        
        reason_text = "; ".join(reasons) if reasons else "Standard module"
        return max(0, priority), reason_text, strategy
    
    def create_conversion_plan(self) -> Dict[str, Any]:
        """Create comprehensive conversion plan"""
        overview = self.get_system_overview()
        candidates = self.analyze_conversion_candidates()
        features = self.get_features_map()
        
        # Group candidates by strategy
        strategy_groups = {}
        for candidate in candidates:
            strategy = candidate.conversion_strategy
            if strategy not in strategy_groups:
                strategy_groups[strategy] = []
            strategy_groups[strategy].append(candidate)
        
        # Create conversion phases
        phases = self._create_conversion_phases(candidates)
        
        return {
            "timestamp": datetime.utcnow().isoformat(),
            "system_overview": overview["system"],
            "total_candidates": len(candidates),
            "conversion_phases": phases,
            "strategy_groups": {
                strategy: [
                    {
                        "id": c.id,
                        "name": c.name,
                        "priority": c.priority,
                        "reason": c.reason,
                        "features": c.features,
                        "routes": c.routes,
                        "is_hot_reloadable": c.is_hot_reloadable
                    }
                    for c in candidates
                ]
                for strategy, candidates in strategy_groups.items()
            },
            "recommendations": self._generate_recommendations(candidates, features)
        }
    
    def _create_conversion_phases(self, candidates: List[ModuleConversionCandidate]) -> List[Dict[str, Any]]:
        """Create conversion phases based on priority and dependencies"""
        phases = []
        
        # Phase 1: High-priority, low-dependency modules
        phase1 = [c for c in candidates if c.priority >= 20 and c.is_hot_reloadable]
        if phase1:
            phases.append({
                "phase": 1,
                "name": "Quick Wins - Hot-Reload Ready",
                "description": "Convert modules that are already hot-reloadable and high priority",
                "modules": [{"id": c.id, "name": c.name, "priority": c.priority} for c in phase1],
                "estimated_effort": "Low",
                "timeline": "1-2 days"
            })
        
        # Phase 2: High-priority modules needing hot-reload setup
        phase2 = [c for c in candidates if c.priority >= 15 and not c.is_hot_reloadable and "test" not in c.id.lower()]
        if phase2:
            phases.append({
                "phase": 2,
                "name": "High-Impact Conversions",
                "description": "Convert high-priority modules that need hot-reload setup",
                "modules": [{"id": c.id, "name": c.name, "priority": c.priority} for c in phase2[:5]],  # Top 5
                "estimated_effort": "Medium",
                "timeline": "1-2 weeks"
            })
        
        # Phase 3: Medium-priority modules
        phase3 = [c for c in candidates if 10 <= c.priority < 15]
        if phase3:
            phases.append({
                "phase": 3,
                "name": "Medium-Priority Conversions",
                "description": "Convert medium-priority modules for broader coverage",
                "modules": [{"id": c.id, "name": c.name, "priority": c.priority} for c in phase3[:8]],  # Top 8
                "estimated_effort": "Medium",
                "timeline": "2-3 weeks"
            })
        
        # Phase 4: Remaining modules
        phase4 = [c for c in candidates if c.priority < 10 and c.priority > 0]
        if phase4:
            phases.append({
                "phase": 4,
                "name": "Complete Coverage",
                "description": "Convert remaining modules for complete spec-driven coverage",
                "modules": [{"id": c.id, "name": c.name, "priority": c.priority} for c in phase4],
                "estimated_effort": "High",
                "timeline": "1-2 months"
            })
        
        return phases
    
    def _generate_recommendations(self, candidates: List[ModuleConversionCandidate], 
                                features: List[Dict[str, Any]]) -> List[str]:
        """Generate recommendations based on analysis"""
        recommendations = []
        
        # Analyze current state
        total_modules = len(candidates) + 3  # +3 for stable modules
        hot_reloadable = len([c for c in candidates if c.is_hot_reloadable])
        high_priority = len([c for c in candidates if c.priority >= 15])
        
        recommendations.append(f"Current system has {total_modules} modules, {hot_reloadable} already hot-reloadable")
        recommendations.append(f"{high_priority} modules identified as high-priority for conversion")
        
        # Feature-based recommendations
        ai_modules = [c for c in candidates if "AI" in c.features]
        if ai_modules:
            recommendations.append(f"Focus on {len(ai_modules)} AI modules for enhanced spec-driven capabilities")
        
        resonance_modules = [c for c in candidates if "Resonance" in c.features]
        if resonance_modules:
            recommendations.append(f"Prioritize {len(resonance_modules)} Resonance modules for U-CORE integration")
        
        # Strategy recommendations
        if hot_reloadable > 0:
            recommendations.append("Start with hot-reloadable modules for quick wins")
        
        if high_priority > 5:
            recommendations.append("Consider parallel conversion of high-priority modules")
        
        recommendations.append("Implement spec-driven metadata tracking for all converted modules")
        recommendations.append("Create automated testing for spec-driven module validation")
        
        return recommendations
    
    def print_conversion_plan(self):
        """Print the conversion plan in a readable format"""
        plan = self.create_conversion_plan()
        
        print("🚀 SPEC-DRIVEN MODULE CONVERSION PLAN")
        print("=" * 50)
        print(f"Generated: {plan['timestamp']}")
        print()
        
        # System overview
        overview = plan["system_overview"]
        print("📊 SYSTEM OVERVIEW")
        print(f"Total Modules: {overview['totalModules']}")
        print(f"Total Routes: {overview['totalRoutes']}")
        print(f"Total Features: {overview['totalFeatures']}")
        print(f"Hot-Reloadable: {overview['hotReloadableModules']}")
        print(f"Stable: {overview['stableModules']}")
        print()
        
        # Conversion phases
        print("📋 CONVERSION PHASES")
        for phase in plan["conversion_phases"]:
            print(f"\nPhase {phase['phase']}: {phase['name']}")
            print(f"Description: {phase['description']}")
            print(f"Effort: {phase['estimated_effort']} | Timeline: {phase['timeline']}")
            print("Modules:")
            for module in phase["modules"]:
                print(f"  - {module['name']} (Priority: {module['priority']})")
        
        # Strategy groups
        print("\n🎯 CONVERSION STRATEGIES")
        for strategy, modules in plan["strategy_groups"].items():
            if modules:
                print(f"\n{strategy.upper()}:")
                for module in modules[:3]:  # Show top 3
                    print(f"  - {module['name']} (Priority: {module['priority']})")
                if len(modules) > 3:
                    print(f"  ... and {len(modules) - 3} more")
        
        # Recommendations
        print("\n💡 RECOMMENDATIONS")
        for i, rec in enumerate(plan["recommendations"], 1):
            print(f"{i}. {rec}")
        
        print("\n" + "=" * 50)
        print("✨ Ready to begin spec-driven conversion!")

def main():
    planner = SpecDrivenConversionPlanner()
    try:
        planner.print_conversion_plan()
    except requests.exceptions.ConnectionError:
        print("❌ Error: Could not connect to the application at http://localhost:5001")
        print("Make sure the application is running and accessible.")
    except Exception as e:
        print(f"❌ Error: {e}")

if __name__ == "__main__":
    main()
