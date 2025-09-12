#!/usr/bin/env python3
"""
Mark modules for spec-driven conversion
Creates spec atoms for modules that need to be converted to spec-driven architecture
"""

import requests
import json
from typing import Dict, Any, List
from datetime import datetime

class ModuleConversionMarker:
    def __init__(self, base_url: str = "http://localhost:5001"):
        self.base_url = base_url
        self.session = requests.Session()
    
    def get_module_details(self, module_id: str) -> Dict[str, Any]:
        """Get detailed information about a specific module"""
        # Get modules
        modules_response = self.session.get(f"{self.base_url}/spec/modules/all")
        modules_response.raise_for_status()
        modules = modules_response.json()["modules"]
        
        # Find the specific module
        module = next((m for m in modules if m["id"] == module_id), None)
        if not module:
            raise ValueError(f"Module {module_id} not found")
        
        # Get routes for this module
        routes_response = self.session.get(f"{self.base_url}/spec/routes/all")
        routes_response.raise_for_status()
        routes = routes_response.json()["routes"]
        
        module_routes = [r for r in routes if r.get("moduleId") == module_id]
        
        return {
            "module": module,
            "routes": module_routes
        }
    
    def create_conversion_spec(self, module_id: str) -> Dict[str, Any]:
        """Create a spec for converting a module to spec-driven architecture"""
        details = self.get_module_details(module_id)
        module = details["module"]
        routes = details["routes"]
        
        # Create the conversion spec
        spec = {
            "id": f"{module_id}.conversion",
            "name": f"{module['name']} - Spec-Driven Conversion",
            "version": "1.0.0",
            "description": f"Spec-driven conversion plan for {module['name']}",
            "originalModule": {
                "id": module["id"],
                "name": module["name"],
                "version": module["version"],
                "description": module["description"],
                "features": module["features"],
                "isHotReloadable": module["isHotReloadable"],
                "isStable": module["isStable"]
            },
            "conversionMetadata": {
                "conversionType": "hot-reload-to-spec-driven",
                "priority": self._calculate_priority(module),
                "strategy": self._determine_strategy(module),
                "status": "pending",
                "createdAt": datetime.utcnow().isoformat(),
                "estimatedEffort": self._estimate_effort(module, routes),
                "dependencies": module["dependencies"],
                "hotReloadReady": module["isHotReloadable"]
            },
            "targetSpec": {
                "architecture": "ice-water-gas",
                "state": "ice",
                "persistence": "atoms-only",
                "regeneration": "full",
                "hotReload": True,
                "specDriven": True
            },
            "routes": [
                {
                    "id": route["id"],
                    "name": route["name"],
                    "path": route["path"],
                    "method": route["method"],
                    "description": route["description"],
                    "tags": route["tags"],
                    "conversionStatus": "pending",
                    "specDriven": False
                }
                for route in routes
            ],
            "conversionSteps": self._generate_conversion_steps(module, routes),
            "validationCriteria": self._generate_validation_criteria(module)
        }
        
        return spec
    
    def _calculate_priority(self, module: Dict[str, Any]) -> int:
        """Calculate conversion priority for the module"""
        priority = 0
        features = module.get("features", [])
        
        if "AI" in features or "LLM" in features:
            priority += 20
        if "Resonance" in features:
            priority += 15
        if "Real-time" in features:
            priority += 12
        if "Translation" in features:
            priority += 10
        if "Security" in features:
            priority += 8
        if "Graph" in features:
            priority += 6
        
        if module.get("isHotReloadable", False):
            priority += 5
        
        if "spec" in module["id"].lower():
            priority += 15
        if "concept" in module["id"].lower():
            priority += 12
        
        return priority
    
    def _determine_strategy(self, module: Dict[str, Any]) -> str:
        """Determine conversion strategy based on module characteristics"""
        features = module.get("features", [])
        module_id = module["id"]
        
        if "Resonance" in features:
            return "resonance-optimized"
        elif "AI" in features or "LLM" in features:
            return "ai-enhanced"
        elif "Real-time" in features:
            return "realtime-optimized"
        elif "Translation" in features:
            return "translation-optimized"
        elif "Security" in features:
            return "security-focused"
        elif "Graph" in features:
            return "graph-optimized"
        elif "spec" in module_id.lower():
            return "spec-native"
        elif "concept" in module_id.lower():
            return "concept-optimized"
        elif "test" in module_id.lower():
            return "test-optimized"
        else:
            return "standard"
    
    def _estimate_effort(self, module: Dict[str, Any], routes: List[Dict[str, Any]]) -> str:
        """Estimate conversion effort"""
        route_count = len(routes)
        features = module.get("features", [])
        
        if route_count > 10 or len(features) > 5:
            return "High"
        elif route_count > 5 or len(features) > 3:
            return "Medium"
        else:
            return "Low"
    
    def _generate_conversion_steps(self, module: Dict[str, Any], routes: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        """Generate step-by-step conversion plan"""
        steps = [
            {
                "step": 1,
                "name": "Create Spec Atoms",
                "description": "Extract current module structure and create spec atoms",
                "status": "pending",
                "estimatedTime": "30 minutes"
            },
            {
                "step": 2,
                "name": "Mark as Not Spec-Driven",
                "description": "Add metadata to mark module as not yet spec-driven",
                "status": "pending",
                "estimatedTime": "15 minutes"
            },
            {
                "step": 3,
                "name": "Setup Hot-Reload",
                "description": "Configure module for hot-reload if not already done",
                "status": "pending" if not module.get("isHotReloadable", False) else "completed",
                "estimatedTime": "1 hour" if not module.get("isHotReloadable", False) else "0 minutes"
            },
            {
                "step": 4,
                "name": "Generate Spec-Driven Code",
                "description": "Generate new spec-driven implementation",
                "status": "pending",
                "estimatedTime": "2-4 hours"
            },
            {
                "step": 5,
                "name": "Test and Validate",
                "description": "Test the converted module and validate functionality",
                "status": "pending",
                "estimatedTime": "1-2 hours"
            },
            {
                "step": 6,
                "name": "Deploy and Monitor",
                "description": "Deploy converted module and monitor performance",
                "status": "pending",
                "estimatedTime": "30 minutes"
            }
        ]
        
        return steps
    
    def _generate_validation_criteria(self, module: Dict[str, Any]) -> List[str]:
        """Generate validation criteria for the conversion"""
        criteria = [
            "All existing routes must be preserved",
            "Module functionality must remain unchanged",
            "Performance must be maintained or improved",
            "Hot-reload capability must be working",
            "Spec atoms must be properly stored and retrievable",
            "Module must be marked as spec-driven in metadata"
        ]
        
        features = module.get("features", [])
        if "AI" in features:
            criteria.append("AI functionality must be preserved and enhanced")
        if "Resonance" in features:
            criteria.append("Resonance calculations must be accurate")
        if "Real-time" in features:
            criteria.append("Real-time performance must be maintained")
        
        return criteria
    
    def submit_conversion_spec(self, module_id: str) -> Dict[str, Any]:
        """Submit a conversion spec to the spec system"""
        spec = self.create_conversion_spec(module_id)
        
        # Create atoms structure
        atoms = {
            "nodes": [
                {
                    "id": spec["id"],
                    "typeId": "codex.spec/conversion",
                    "state": "ice",
                    "locale": "en",
                    "title": spec["name"],
                    "description": spec["description"],
                    "content": {
                        "spec": spec,
                        "conversionType": "hot-reload-to-spec-driven",
                        "priority": spec["conversionMetadata"]["priority"],
                        "strategy": spec["conversionMetadata"]["strategy"]
                    }
                }
            ],
            "edges": []
        }
        
        # Submit to spec system
        response = self.session.post(
            f"{self.base_url}/spec/atoms",
            json={
                "moduleId": f"{module_id}.conversion",
                "atoms": atoms
            },
            headers={"Content-Type": "application/json"}
        )
        
        if response.status_code == 200:
            return {
                "success": True,
                "message": f"Conversion spec created for {module_id}",
                "specId": f"{module_id}.conversion",
                "priority": spec["conversionMetadata"]["priority"],
                "strategy": spec["conversionMetadata"]["strategy"]
            }
        else:
            return {
                "success": False,
                "error": f"Failed to create conversion spec: {response.text}"
            }
    
    def mark_module_as_not_spec_driven(self, module_id: str) -> Dict[str, Any]:
        """Mark a module as not yet spec-driven in the spec system"""
        # This would update the module's metadata to indicate it's not spec-driven
        # For now, we'll create a tracking node
        atoms = {
            "nodes": [
                {
                    "id": f"{module_id}.tracking",
                    "typeId": "codex.spec/tracking",
                    "state": "ice",
                    "locale": "en",
                    "title": f"Tracking for {module_id}",
                    "description": f"Tracks conversion status for {module_id}",
                    "content": {
                        "moduleId": module_id,
                        "isSpecDriven": False,
                        "conversionStatus": "pending",
                        "markedAt": datetime.utcnow().isoformat()
                    }
                }
            ],
            "edges": []
        }
        
        response = self.session.post(
            f"{self.base_url}/spec/atoms",
            json={
                "moduleId": f"{module_id}.tracking",
                "atoms": atoms
            },
            headers={"Content-Type": "application/json"}
        )
        
        if response.status_code == 200:
            return {
                "success": True,
                "message": f"Module {module_id} marked as not spec-driven",
                "trackingId": f"{module_id}.tracking"
            }
        else:
            return {
                "success": False,
                "error": f"Failed to mark module: {response.text}"
            }

def main():
    marker = ModuleConversionMarker()
    
    # High-priority modules to mark for conversion
    high_priority_modules = [
        "codex.joy",  # Joy and Resonance Module (Priority: 32)
        "codex.concept",  # Concept Management Module (Priority: 29)
        "codex.userconcept",  # User-Concept Relationship Module (Priority: 29)
        "codex.breath",  # Breath Engine Module (Priority: 20)
        "codex.composer"  # Spec Composer Module (Priority: 20)
    ]
    
    print("🚀 MARKING MODULES FOR SPEC-DRIVEN CONVERSION")
    print("=" * 50)
    
    for module_id in high_priority_modules:
        print(f"\n📋 Processing {module_id}...")
        
        try:
            # Mark as not spec-driven
            tracking_result = marker.mark_module_as_not_spec_driven(module_id)
            if tracking_result["success"]:
                print(f"  ✅ Marked as not spec-driven: {tracking_result['trackingId']}")
            else:
                print(f"  ❌ Failed to mark: {tracking_result['error']}")
                continue
            
            # Create conversion spec
            spec_result = marker.submit_conversion_spec(module_id)
            if spec_result["success"]:
                print(f"  ✅ Conversion spec created: {spec_result['specId']}")
                print(f"  📊 Priority: {spec_result['priority']}")
                print(f"  🎯 Strategy: {spec_result['strategy']}")
            else:
                print(f"  ❌ Failed to create spec: {spec_result['error']}")
        
        except Exception as e:
            print(f"  ❌ Error processing {module_id}: {e}")
    
    print("\n" + "=" * 50)
    print("✨ High-priority modules marked for conversion!")
    print("\nNext steps:")
    print("1. Review conversion specs in the spec system")
    print("2. Begin with highest priority modules")
    print("3. Use hot-reload for testing conversions")
    print("4. Validate functionality after each conversion")

if __name__ == "__main__":
    main()
