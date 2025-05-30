use std::{env, process::Command};
use camino::Utf8Path;

fn main() {
        println!("cargo:rerun-if-changed=build.rs");
        let out_dir = "bindings";
        let udl_file = "src/snapxrust.udl";
        let cargo = Utf8Path::new("Cargo.toml");
        uniffi_build::generate_scaffolding(udl_file).unwrap();
            if Command::new("$HOME/.cargo/bin/uniffi-bindgen-cs").arg("--version").output().is_err() {
        println!("Installing uniffi-bindgen-cs!");
        Command::new("cargo")
            .arg("install")
            .arg("uniffi-bindgen-cs")
            .arg("--git")
            .arg("https://github.com/NordSecurity/uniffi-bindgen-cs")
            .arg("--tag")
            .arg("v0.9.1+v0.28.3")
            .status().expect("Failed to install uniffi-bindgen-cs");
        }
        Command::new("$HOME/.cargo/bin/uniffi-bindgen-cs").arg(udl_file)
        .arg("--config").arg(cargo).arg("--out-dir").arg(out_dir).output();
}
