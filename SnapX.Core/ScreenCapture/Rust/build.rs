use std::process::Command;
use camino::Utf8Path;

fn main() {
        println!("cargo:rerun-if-changed=build.rs");
        let out_dir = "bindings";
        let udl_file = "src/snapxrust.udl";
        let cargo = Utf8Path::new("Cargo.toml");
        uniffi_build::generate_scaffolding(udl_file).unwrap();
            // if Command::new("$HOME/.cargo/bin/uniffi-bindgen-cs").arg("--version").output().is_err() {
        // Using fork to bump uniffi to 0.30
        println!("Installing uniffi-bindgen-cs updated fork!");
            Command::new("cargo")
                .arg("install")
                .arg("uniffi-bindgen-cs")
                .arg("--git")
                .arg("https://github.com/sensslen/uniffi-bindgen-cs")
                .arg("--branch")
                .arg("main")
                .status()
                .expect("Failed to install uniffi-bindgen-cs");
        // }
        let _ = Command::new("$HOME/.cargo/bin/uniffi-bindgen-cs").arg(udl_file)
        .arg("--config").arg(cargo).arg("--out-dir").arg(out_dir).output();
}
